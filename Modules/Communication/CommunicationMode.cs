namespace HRMS.Modules.Communication;

//--------------------------------------------------------------------------------//
// 압축기 하나를 실제로 통신할지, 모의값으로 대신할지 정한다.
//
//   Communication:TestMode = false  → 전부 실제 통신
//   Communication:TestMode = true   → 전부 모의값. 단, RealDeviceIps에 적힌 IP만 실제 통신
//
// RealDeviceIps는 개발 중 샘플 TLC 한두 대만 실기기로 붙여서 테스트하기 위한 것이다
// (2026-09-29 추가). TestMode를 통째로 끄면 나머지 압축기가 전부 끊김이 되어 30초 뒤
// 통신장애 경보가 200여 건 쌓이고, 프론트 화면이 경보로 뒤덮여 테스트를 할 수 없다.
// 운영 환경에서는 TestMode가 false라 이 목록은 쓰이지 않는다.
//
// 폴링(읽기)과 비상정지(쓰기)가 같은 기준을 써야 해서 한 곳에 둔다 — 하나만 실제로
// 통신하면 "명령은 실제로 갔는데 화면은 모의값" 같은 어긋남이 생긴다.
//--------------------------------------------------------------------------------//
public static class CommunicationMode
{
    public static bool IsTestMode(IConfiguration configuration) =>
        configuration.GetValue("Communication:TestMode", false);

    public static bool IsSimulated(IConfiguration configuration, string? ipAddress)
    {
        if (!IsTestMode(configuration))
            return false;

        var realDeviceIps = configuration.GetSection("Communication:RealDeviceIps").Get<string[]>() ?? [];
        return ipAddress is null || !realDeviceIps.Contains(ipAddress);
    }
}
