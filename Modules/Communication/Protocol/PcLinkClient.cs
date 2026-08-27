using System.Net.Sockets;
using System.Text;

namespace HRMS.Modules.Communication.Protocol;

//--------------------------------------------------------------------------------//
// 삼원테크 PC-LINK SUM 프로토콜(TCP, ASCII 기반)로 압축기(TLC)와 통신한다.
// 이 시스템에서 실제로 쓰는 명령은 "CH01~07 + UNUSED + DOSTS 9개 레지스터 읽기"
// 자세한 프레임 구조와 예제는 Doc/pclink protocol.md 참고.
//--------------------------------------------------------------------------------//
public static class PcLinkClient
{
    public const int Port = 5000;

    // 국번, 전 압축기 공통(overview.md 4.2)
    private const string Station = "01"; 

    //--------------------------------------------------------------------------------//
    // CH01~07, UNUSED, DOSTS
    // 고온, 저온, 흡입, 응축, 운전전류, 운전시간, 경보상태, DOSTS
    //--------------------------------------------------------------------------------//
    private const string ReadRegisters = "0360,0361,0362,0364,0365,0366,0367,0099,1805"; 
    private const int ReadCount = 9;

    private const byte Stx = 0x02;
    private const byte Cr = 0x0D;
    private const byte Lf = 0x0A;

    //--------------------------------------------------------------------------------//
    // 압축기 1대에 연결해서 9개 레지스터를 한 번 읽어온다.
    // 연결/응답 각각 timeoutMs 안에 안 끝나면 실패(Ok=false)로 처리하고, 예외를 던지지 않는다.
    // (호출부인 CompressorPollingService가 압축기별로 동시에 이 메서드를 호출하므로,
    //  한 대가 느려도 예외 없이 그냥 실패로 끝나야 다른 압축기 폴링에 영향을 안 준다.)
    //--------------------------------------------------------------------------------//
    public static async Task<(bool Ok, short[] Values, string Raw)> ReadChannelsAsync(string ipAddress, int timeoutMs = 3000)
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(ipAddress, Port);
        if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) != connectTask)
            return (false, [], "connect timeout");

        using var stream = client.GetStream();
        await stream.WriteAsync(BuildReadCommand());

        var buffer = new byte[512];
        var readTask = stream.ReadAsync(buffer).AsTask();
        if (await Task.WhenAny(readTask, Task.Delay(timeoutMs)) != readTask)
            return (false, [], "response timeout");

        int len = await readTask;
        return ParseResponse(buffer, len);
    }

    //--------------------------------------------------------------------------------//
    // 프레임 구조: [STX] 국번+커맨드(RRD) , 개수 , 레지스터... {체크섬} [CR][LF]
    // 예: [STX]01RRD,009,0360,...,1805{XX}[CR][LF]
    //--------------------------------------------------------------------------------//
    private static byte[] BuildReadCommand()
    {
        string payload = $"{Station}RRD,{ReadCount:D3},{ReadRegisters}";
        string frame = payload + ComputeChecksum(payload);

        var bytes = new byte[frame.Length + 3];
        bytes[0] = Stx;
        Encoding.ASCII.GetBytes(frame, 0, frame.Length, bytes, 1);
        bytes[^2] = Cr;
        bytes[^1] = Lf;
        return bytes;
    }

    //--------------------------------------------------------------------------------//
    // 응답 예: [STX]01RRD,OK,0000,041A,...,0000{체크섬}[CR][LF]
    // OK가 아니거나 체크섬이 안 맞으면 전부 실패(Ok=false)로 취급한다 
    // 세부 에러 코드는 구분하지 않는다(단순화 원칙).
    //--------------------------------------------------------------------------------//
    private static (bool Ok, short[] Values, string Raw) ParseResponse(byte[] buffer, int len)
    {
        string raw = Encoding.ASCII.GetString(buffer, 0, len);

        if (len < 5 || buffer[0] != Stx)
            return (false, [], raw);

        string content = Encoding.ASCII.GetString(buffer, 1, len - 1).TrimEnd('\r', '\n');
        if (content.Length < 2)
            return (false, [], raw);

        //--------------------------------------------------------------------------------//
        // 체크섬(2자리 16진수)은 콤마 없이 데이터 끝에 바로 붙어있다.
        // 체크섬이 안 맞으면 전부 실패(Ok=false)로 취급한다 — 세부 에러 코드는 구분하지 않는다(단순화 원칙).
        //--------------------------------------------------------------------------------//
        string payload = content[..^2];
        string receivedChecksum = content[^2..];
        if (ComputeChecksum(payload) != receivedChecksum)
            return (false, [], raw);

        var fields = payload.Split(',');
        if (fields.Length < 2 || fields[1] != "OK")
            return (false, [], raw);

        //--------------------------------------------------------------------------------//
        // 4자리 16진수를 16비트 2의 보수로 해석해 음수(예: 영하 온도)도 정확히 변환한다.
        // (Convert.ToInt32를 쓰면 4자리는 부호 판단이 안 돼 음수가 큰 양수로 잘못 읽힌다.)
        // 원시값(raw int16)을 그대로 반환한다 — 소수점 스케일링은 백엔드에서 하지 않고 프론트가 담당한다.
        //--------------------------------------------------------------------------------//
        var values = fields.Skip(2)
            .Select(hex => Convert.ToInt16(hex, 16))
            .ToArray();

        return (true, values, raw);
    }

    //--------------------------------------------------------------------------------//
    // SUM = (STX/CR/LF를 뺀 나머지 문자들의 ASCII 값 합) mod 256, 2자리 대문자 16진수.
    // 문서에 공식이 안 적혀있어 프로토콜 문서의 예제 3개로 역산해서 검증한 방식이다.
    //--------------------------------------------------------------------------------//
    private static string ComputeChecksum(string payload)
    {
        int sum = 0;
        foreach (char c in payload) sum += c;
        return (sum % 256).ToString("X2");
    }
}
