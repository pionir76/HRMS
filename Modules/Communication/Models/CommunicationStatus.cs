namespace HRMS.Modules.Communication.Models;

// 압축기 통신 상태. CompressorPollingService가 매 폴링 사이클마다 결정한다:
// 성공하면 연결됨, 직전이 연결됨이었는데 실패하면 재접속중(막 끊긴 상태),
// 그 외 실패는 끊김(원래도 안 됐던 상태)으로 처리한다.
public enum CommunicationStatus
{
    연결됨,
    끊김,
    재접속중
}
