using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Operation.Models;

namespace HRMS.Modules.Trend.Models;

// 1분 단위 트렌드(DailyTrend) 기록. 압축기 1대당 그 분 정각에 정확히 1행.
// 채널을 행이 아니라 컬럼으로 펼친 이유(overview.md 8.1): 채널당 별도 행으로 만들면
// 하루에만 압축기 수 x 7채널 x 1440분만큼 행이 생겨 7배로 불어난다.
// CompressorSensorCurrent와 달리 이 테이블은 계속 누적된다(덮어쓰지 않음).
public class CompressorMeasurement
{
    public int CompressorId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } // 항상 정각(초=0)값

    public decimal? Ch01 { get; set; }
    public decimal? Ch02 { get; set; }
    public decimal? Ch03 { get; set; }
    public decimal? Ch04 { get; set; }
    public decimal? Ch05 { get; set; }
    public decimal? Ch06 { get; set; }
    public decimal? Ch07 { get; set; }

    // 그 순간의 상태 스냅샷. 통신장애로 값이 그대로 유지된 구간인지 트렌드 화면에서 구분하기 위함.
    public RunningStatus RunningStatus { get; set; }
    public AlarmStatus AlarmStatus { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; }
}
