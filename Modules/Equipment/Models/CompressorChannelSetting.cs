namespace HRMS.Modules.Equipment.Models;

// 압축기 1대당 CH01~CH07 정확히 7행을 갖는 채널별 설정.
// 채널명·측정단위처럼 전 압축기 공통인 값은 여기 없고(overview.md 8.1의 채널 정의 표 참고),
// 압축기마다 달라질 수 있는 값(사용 여부, 경보 기준, 표시 소수점)만 담는다.
public class CompressorChannelSetting
{
    public int CompressorId { get; set; }
    public ChannelNo ChannelNo { get; set; }

    public bool Enabled { get; set; } = true; // 센서 미설치 등으로 꺼두면 경보/상태 판정에서 제외

    // 채널 원시값(raw int16)과 직접 비교하는 경보 상/하한 — raw 스케일이며 소수점 가공 없음.
    public short? LowerLimit { get; set; }
    public short? UpperLimit { get; set; }
    public bool AlarmEnabled { get; set; } = true;
    public int? AlarmDelaySeconds { get; set; }
    public int? AlarmClearDelaySeconds { get; set; }
    public int DecimalPlaces { get; set; } = 1; // 표시 소수점 자리수
}
