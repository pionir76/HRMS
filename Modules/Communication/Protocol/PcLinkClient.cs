using System.Net.Sockets;
using System.Text;

namespace HRMS.Modules.Communication.Protocol;

//--------------------------------------------------------------------------------//
// 삼원테크 PC-LINK SUM 프로토콜(TCP, ASCII 기반)로 압축기(TLC)와 통신한다.
// 이 시스템에서 실제로 쓰는 명령은 "CH01~07 + DIOSTS 8개 레지스터 읽기" 하나뿐이다.
// 자세한 프레임 구조와 예제는 Doc/pclink protocol.md 참고.
//
// **읽을 주소는 압축기마다 다를 수 있다**(사양 확정 2026-09-18). 표준 장비는
// 360/361/362/364/365/366/367이지만, LG 냉동기는 TLC 1대가 압축기 42대를 중계하면서
// 압축기마다 주소가 다르다. 그래서 주소를 상수로 두지 않고 호출부(폴링 서비스)가
// 채널 설정(CompressorChannelSetting.RegisterAddress)에서 읽어 넘긴다.
//
// TLC 단위로 묶어 한 번에 읽는 방식도 검토했지만, 압축기 개별 통신을 유지하기로 했다
// (사용자 결정 2026-09-18 — 예외를 최소화하고 코드 경로를 하나로 유지). 요청당 8개라
// RRD의 64개 제한에도 걸리지 않는다.
//--------------------------------------------------------------------------------//
public static class PcLinkClient
{
    public const int Port = 5000;

    // 국번, 전 압축기 공통(overview.md 4.2)
    private const string Station = "01";

    //--------------------------------------------------------------------------------//
    // 비상정지 DO 출력 레지스터. 명령을 쓰는 곳이자 상태를 읽는 곳이다 — 0001=정지, 0000=해제이며
    // 전 장비 공통이다(LG 냉동기도 동일 — 사용자 확인 2026-09-18).
    //
    // 센서 7개와 함께 매번 읽어서 8번째 값으로 받는다. 원래 사양은 상태 판정을 D1801(DIOSTS)의
    // 비트 8로 하는 것이었는데, 실기기 시험에서 **D1805에 0001을 써도 D1801은 0x0000 그대로**인
    // 것을 확인했다(2026-09-29, 샘플 TLC). D1801은 실제 접점(DI) 상태를 비추는 것으로 보이며
    // 배선 없이는 올라오지 않는다. 예전 시스템의 조회 명령도 D1801이 아니라 D1805를 읽고 있었다.
    // 그래서 판정 기준을 D1805로 확정했다(사용자 결정 2026-09-29).
    //--------------------------------------------------------------------------------//
    public const int DoOutputRegister = 1805;

    public static bool IsEmergencyStopped(short doOutput) => doOutput != 0;

    //--------------------------------------------------------------------------------//
    // 센서가 없는 채널 자리에 넣는 더미 주소. 값은 쓰지 않고 버린다 — 요청/응답 길이를
    // 전 압축기 8개로 고정해서 인덱스가 밀리지 않게 하려는 목적이다(protocol.md의 UNUSED).
    //--------------------------------------------------------------------------------//
    private const int UnusedRegister = 99;

    public const int ChannelCount = 7;

    //--------------------------------------------------------------------------------//
    // 기본 타임아웃. 사내 LAN이라 정상 응답은 수십 ms 수준이다. 예전 기본값(3초)은 폴링
    // 주기(3초)보다 길어서, 한 대만 응답이 없어도 사이클이 6초 넘게 늘어났다
    // (연결 3초 + 응답 3초). appsettings의 Communication:TimeoutMs로 조정할 수 있다.
    //--------------------------------------------------------------------------------//
    public const int DefaultTimeoutMs = 1500;

    //--------------------------------------------------------------------------------//
    // 실패 사유. 예전에는 모든 실패가 Ok=false 한 비트로 뭉개져서, 현장에서 값이 안 들어올 때
    // 원인(케이블/전원/방화벽 vs 프로토콜 이상)을 구분할 수 없었다(overview.md 4.2가 요구하는
    // "비정상 응답 로그"도 이 때문에 불가능했다).
    //--------------------------------------------------------------------------------//
    public enum ReadFailureReason
    {
        없음,
        연결실패,       // 대상이 응답하지 않음(전원/케이블/IP/방화벽)
        연결타임아웃,
        응답타임아웃,
        연결끊김,       // 요청은 보냈는데 응답 도중 연결이 끊김
        체크섬오류,
        오류응답,       // 장비가 OK가 아닌 응답을 보냄
        값개수부족      // OK인데 요청한 개수만큼 값이 오지 않음
    }

    public readonly record struct ReadResult(bool Ok, short[] Values, ReadFailureReason Reason, string Raw);

    //--------------------------------------------------------------------------------//
    // 쓰기 결과. SentCommand는 실제로 보낸 명령문(체크섬 제외)이며, 비상정지 이력에 "무엇을
    // 보냈는지"를 남기기 위한 것이다(overview.md 4.8 "TCP로 전송한 명령 정보를 기록").
    //--------------------------------------------------------------------------------//
    public readonly record struct WriteResult(bool Ok, ReadFailureReason Reason, string SentCommand, string Raw);

    // ExchangeAsync의 내부 반환값 — 연결/송수신까지만 성공했는지와 받은 바이트다(해석 전).
    private readonly record struct Exchange(bool Ok, byte[] Buffer, int Len, ReadFailureReason Reason, string Raw);

    private const byte Stx = 0x02;
    private const byte Cr = 0x0D;
    private const byte Lf = 0x0A;

    //--------------------------------------------------------------------------------//
    // 압축기 1대에 연결해서 8개 레지스터(CH01~07 + DIOSTS)를 한 번 읽어온다.
    // channelRegisters는 CH01~CH07 순서의 주소 7개이고, null인 자리는 센서가 없다는 뜻이라
    // 더미 주소를 넣어 읽고 값은 버린다(호출부가 그 채널을 저장하지 않는다).
    // 연결/응답 각각 timeoutMs 안에 안 끝나면 실패(Ok=false)로 처리하고, 예외를 던지지 않는다.
    // (호출부인 CompressorPollingService가 압축기별로 동시에 이 메서드를 호출하므로,
    //  한 대가 느려도 예외 없이 그냥 실패로 끝나야 다른 압축기 폴링에 영향을 안 준다.)
    //--------------------------------------------------------------------------------//
    public static async Task<ReadResult> ReadChannelsAsync(
        string ipAddress, IReadOnlyList<int?> channelRegisters, int timeoutMs = DefaultTimeoutMs,
        CancellationToken stoppingToken = default)
    {
        var exchange = await ExchangeAsync(ipAddress, BuildReadCommand(channelRegisters), timeoutMs, stoppingToken);
        return exchange.Ok
            ? ParseReadResponse(exchange.Buffer, exchange.Len, ChannelCount + 1)
            : new ReadResult(false, [], exchange.Reason, exchange.Raw);
    }

    //--------------------------------------------------------------------------------//
    // 비상정지 상태(D1805)만 한 개 읽는다. 비상정지 명령을 보낸 직후 "정말 바뀌었는지"를
    // 확인하는 용도다 — 폴링도 3초마다 같은 값을 읽지만, 명령 응답을 기다리는 사용자에게
    // 다음 사이클까지 기다리게 할 수 없다(overview.md 4.8 "실제 정지되었는지 확인").
    //--------------------------------------------------------------------------------//
    public static async Task<ReadResult> ReadEmergencyStopAsync(
        string ipAddress, int timeoutMs = DefaultTimeoutMs, CancellationToken stoppingToken = default)
    {
        var exchange = await ExchangeAsync(ipAddress, BuildSingleReadCommand(DoOutputRegister), timeoutMs, stoppingToken);
        return exchange.Ok
            ? ParseReadResponse(exchange.Buffer, exchange.Len, 1)
            : new ReadResult(false, [], exchange.Reason, exchange.Raw);
    }

    //--------------------------------------------------------------------------------//
    // D-Register 한 개에 값을 쓴다(WRD). 이 시스템에서 쓰기는 비상정지(D1805)가 유일하다 —
    // TLC 프로토콜이 제공하는 쓰기 기능 자체가 DO 접점 출력 하나뿐이다(overview.md 4.8).
    //--------------------------------------------------------------------------------//
    public static async Task<WriteResult> WriteRegisterAsync(
        string ipAddress, int register, ushort value, int timeoutMs = DefaultTimeoutMs,
        CancellationToken stoppingToken = default)
    {
        string command = BuildWriteCommandText(register, value);
        var exchange = await ExchangeAsync(ipAddress, ToFrame(command), timeoutMs, stoppingToken);

        if (!exchange.Ok)
            return new WriteResult(false, exchange.Reason, command, exchange.Raw);

        var (ok, reason, raw) = ParseWriteResponse(exchange.Buffer, exchange.Len);
        return new WriteResult(ok, reason, command, raw);
    }

    //--------------------------------------------------------------------------------//
    // 연결 → 프레임 전송 → [CR][LF]까지 수신. 읽기와 쓰기가 같은 절차라 여기 모은다.
    //--------------------------------------------------------------------------------//
    private static async Task<Exchange> ExchangeAsync(
        string ipAddress, byte[] frame, int timeoutMs, CancellationToken stoppingToken)
    {
        //--------------------------------------------------------------------------------//
        // 타임아웃과 앱 종료를 하나의 토큰으로 묶는다. 예전에는 Task.WhenAny(작업, Task.Delay)
        // 방식이라 (a) 정상 응답이어도 타이머가 만료될 때까지 남고 (b) 타임아웃 시 버려진
        // 작업이 관찰되지 않은 예외로 끝났으며 (c) 앱 종료 시 최대 6초 매달렸다.
        //--------------------------------------------------------------------------------//
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(timeoutMs);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(ipAddress, Port, cts.Token);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            return new Exchange(false, [], 0, ReadFailureReason.연결타임아웃, "");
        }
        catch (SocketException ex)
        {
            return new Exchange(false, [], 0, ReadFailureReason.연결실패, ex.SocketErrorCode.ToString());
        }

        using var stream = client.GetStream();

        try
        {
            await stream.WriteAsync(frame, cts.Token);

            //--------------------------------------------------------------------------------//
            // TCP는 메시지 경계를 보장하지 않으므로 [CR][LF]가 나올 때까지 이어 읽는다.
            // 예전에는 ReadAsync를 한 번만 호출해서, 프레임이 두 조각으로 나뉘어 도착하면
            // 앞 조각만 파싱되어 체크섬 불일치로 그 사이클 값이 통째로 유실됐다.
            //--------------------------------------------------------------------------------//
            var buffer = new byte[512];
            int len = 0;
            while (len < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(len), cts.Token);
                if (read == 0)
                    return new Exchange(false, [], 0, ReadFailureReason.연결끊김, Encoding.ASCII.GetString(buffer, 0, len));

                len += read;
                if (buffer[len - 1] == Lf)
                    break;
            }

            return new Exchange(true, buffer, len, ReadFailureReason.없음, Encoding.ASCII.GetString(buffer, 0, len));
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            return new Exchange(false, [], 0, ReadFailureReason.응답타임아웃, "");
        }
        catch (IOException ex)
        {
            return new Exchange(false, [], 0, ReadFailureReason.연결끊김, ex.Message);
        }
    }

    //--------------------------------------------------------------------------------//
    // 프레임 구조: [STX] 국번+커맨드(RRD) , 개수 , 레지스터... {체크섬} [CR][LF]
    // 예: [STX]01RRD,008,0360,0361,0362,0364,0365,0366,0367,1805{XX}[CR][LF]
    // 개수 필드는 3자리다(문서 표에는 2자리로 적혀 있지만 실제 장비는 3자리 — protocol.md 참고).
    //--------------------------------------------------------------------------------//
    private static byte[] BuildReadCommand(IReadOnlyList<int?> channelRegisters)
    {
        var registers = Enumerable.Range(0, ChannelCount)
            .Select(i => (i < channelRegisters.Count ? channelRegisters[i] : null) ?? UnusedRegister)
            .Append(DoOutputRegister);

        return ToFrame($"{Station}RRD,{ChannelCount + 1:D3},{string.Join(",", registers.Select(r => r.ToString("D4")))}");
    }

    // 레지스터 한 개만 읽는 명령. 예: [STX]01RRD,001,1805{XX}[CR][LF]
    private static byte[] BuildSingleReadCommand(int register) =>
        ToFrame($"{Station}RRD,001,{register:D4}");

    //--------------------------------------------------------------------------------//
    // 쓰기 명령문. 예: 01WRD,001,1805,0001 (비상정지) / 01WRD,001,1805,0000 (해제)
    // 체크섬을 붙이기 전의 문자열이라 그대로 이력에 남길 수 있다.
    //--------------------------------------------------------------------------------//
    private static string BuildWriteCommandText(int register, ushort value) =>
        $"{Station}WRD,001,{register:D4},{value:X4}";

    // 공통 프레임 조립: [STX] + 본문 + 체크섬 + [CR][LF]
    private static byte[] ToFrame(string payload)
    {
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
    private static ReadResult ParseReadResponse(byte[] buffer, int len, int expectedCount)
    {
        var (ok, fields, reason, raw) = ParseEnvelope(buffer, len);
        if (!ok)
            return new ReadResult(false, [], reason, raw);

        //--------------------------------------------------------------------------------//
        // 4자리 16진수를 16비트 2의 보수로 해석해 음수(예: 영하 온도)도 정확히 변환한다.
        // (Convert.ToInt32를 쓰면 4자리는 부호 판단이 안 돼 음수가 큰 양수로 잘못 읽힌다.)
        // 원시값(raw int16)을 그대로 반환한다 — 소수점 스케일링은 백엔드에서 하지 않고 프론트가 담당한다.
        // 16진수가 아닌 값이 섞여 있으면(잘린 프레임 등) 예외 대신 오류응답으로 처리한다.
        //--------------------------------------------------------------------------------//
        short[] values;
        try
        {
            values = fields.Skip(2).Select(hex => Convert.ToInt16(hex, 16)).ToArray();
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            return new ReadResult(false, [], ReadFailureReason.오류응답, raw);
        }

        //--------------------------------------------------------------------------------//
        // 요청한 개수(채널 조회는 CH01~07 + DIOSTS = 8)만큼 왔는지 확인한다. 이 검사가 없으면
        // 잘린 응답이 "정상 통신"으로 처리되어, 통신 상태는 연결됨인데 값만 멈춘 채 옛 값으로
        // 경보 판정이 계속된다(2026-09-28 가짜 TLC로 재현 확인).
        //--------------------------------------------------------------------------------//
        if (values.Length < expectedCount)
            return new ReadResult(false, values, ReadFailureReason.값개수부족, raw);

        return new ReadResult(true, values, ReadFailureReason.없음, raw);
    }

    //--------------------------------------------------------------------------------//
    // 쓰기 응답: [STX]01WRD,OK{체크섬}[CR][LF] — 값이 없고 OK만 온다.
    //--------------------------------------------------------------------------------//
    private static (bool Ok, ReadFailureReason Reason, string Raw) ParseWriteResponse(byte[] buffer, int len)
    {
        var (ok, _, reason, raw) = ParseEnvelope(buffer, len);
        return (ok, reason, raw);
    }

    //--------------------------------------------------------------------------------//
    // 읽기/쓰기 공통 봉투 검증 — STX로 시작하는지, 체크섬이 맞는지, 응답이 OK인지.
    // 세부 에러 코드는 구분하지 않는다(단순화 원칙).
    //--------------------------------------------------------------------------------//
    private static (bool Ok, string[] Fields, ReadFailureReason Reason, string Raw) ParseEnvelope(byte[] buffer, int len)
    {
        string raw = Encoding.ASCII.GetString(buffer, 0, len);

        if (len < 5 || buffer[0] != Stx)
            return (false, [], ReadFailureReason.오류응답, raw);

        string content = Encoding.ASCII.GetString(buffer, 1, len - 1).TrimEnd('\r', '\n');
        if (content.Length < 2)
            return (false, [], ReadFailureReason.오류응답, raw);

        //--------------------------------------------------------------------------------//
        // 체크섬(2자리 16진수)은 콤마 없이 데이터 끝에 바로 붙어있다.
        //--------------------------------------------------------------------------------//
        string payload = content[..^2];
        if (ComputeChecksum(payload) != content[^2..])
            return (false, [], ReadFailureReason.체크섬오류, raw);

        var fields = payload.Split(',');
        if (fields.Length < 2 || fields[1] != "OK")
            return (false, fields, ReadFailureReason.오류응답, raw);

        return (true, fields, ReadFailureReason.없음, raw);
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
