namespace HRMS.Modules.Equipment.Models;

// 압축기 1대당 CH01~CH07 정확히 7행을 갖는 채널별 설정.
public class CompressorChannelSetting
{
    public int CompressorId { get; set; }
    public ChannelNo ChannelNo { get; set; }

    // 채널 한글 명칭/단위. 전 압축기 공통 값(overview.md 8.1)이라 사실상 ChannelNo로 결정되지만,
    // 경보 메시지 생성 시 한 행 조회만으로 다 얻을 수 있도록 여기 같이 저장한다(사용자 결정).
    public string ChannelName { get; set; } = ""; // 예: 저온, 고온, 운전전류
    public string Unit { get; set; } = ""; // 예: ℃, MPa, A

    public bool Enabled { get; set; } = true; // 센서 미설치 등으로 꺼두면 경보/상태 판정에서 제외

    // 채널 원시값(raw int16)과 직접 비교하는 경보 상/하한 — raw 스케일이며 소수점 가공 없음.
    public short? LowerLimit { get; set; }
    public short? UpperLimit { get; set; }
    public bool AlarmEnabled { get; set; } = true;
    public int? AlarmDelaySeconds { get; set; } = 30; // 경보 발생 확정까지 대기 시간(초). 짧은 순간 이탈로 인한 오탐 방지
    public int? AlarmClearDelaySeconds { get; set; } = 30; // 경보 해제 확정까지 대기 시간(초). 짧은 순간 복귀로 인한 오탐 방지
    public int DecimalPlaces { get; set; } = 1; // 표시 소수점 자리수
}
