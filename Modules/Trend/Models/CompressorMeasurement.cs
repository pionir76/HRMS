namespace HRMS.Modules.Trend.Models;

//--------------------------------------------------------------------------------//
// 1분 단위 트렌드(DailyTrend) 기록. 압축기 1대당 그 분 정각에 정확히 1행.
// 항상 정각(초=0)값만 기록한다 — 1분 단위 대표값(overview.md 4.5: 1분 대표값 = 마지막값).
// TLC 원시값(raw int16) 그대로 저장한다 — 소수점 스케일링은 프론트가 담당.
//--------------------------------------------------------------------------------//
public class CompressorMeasurement
{
    public int CompressorId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } 

    public short? Ch01 { get; set; }
    public short? Ch02 { get; set; }
    public short? Ch03 { get; set; }
    public short? Ch04 { get; set; }
    public short? Ch05 { get; set; }
    public short? Ch06 { get; set; }
    public short? Ch07 { get; set; }
    
    public bool IsRunning { get; set; }
    public bool HasAlarm { get; set; }
    public bool IsConnected { get; set; }
}
