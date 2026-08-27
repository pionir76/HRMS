using HRMS.Modules.Alarm.Models;

namespace HRMS.Modules.Equipment.Models;

//--------------------------------------------------------------------------------//    
// 압축기 센서값 최신값 테이블. 압축기 1대당 CH01~CH07 최대 7행이며, CompressorPollingService가 
// 3초마다 UPSERT로 덮어쓴다 — 시간이 지나도 행이 늘어나지 않는다. (누적 이력은 이것과 별개로 만들 예정인 1분 단위 트렌드 테이블의 역할이다.)
// 이 채널의 현재 경보 상태 (Modules/Alarm/AlarmEvaluator.cs가 매 사이클 갱신).
//--------------------------------------------------------------------------------//    
public class CompressorSensorCurrent
{
    public int CompressorId { get; set; }
    public ChannelNo ChannelNo { get; set; }
    public short Value { get; set; }
    public DateTimeOffset MeasuredAt { get; set; } 
    public AlarmStatus AlarmStatus { get; set; }

    //--------------------------------------------------------------------------------//    
    // 지연시간(AlarmDelaySeconds/AlarmClearDelaySeconds) 계산을 위해
    // "경보발생대기"/"정상복귀대기" 상태로 바뀐 시각을 기록해둔다. 그 외 상태에서는 null.
    //--------------------------------------------------------------------------------//    
    public DateTimeOffset? PendingSince { get; set; }
}
