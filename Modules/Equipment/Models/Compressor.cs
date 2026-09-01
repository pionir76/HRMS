using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;

namespace HRMS.Modules.Equipment.Models;

//-----------------------------------------------------------------------------//
// 압축기. 실제 TLC 장비와 1:1로 통신하는 단위이며, 장비 내에서 순번(1부터)으로 구분한다. (overview.md 4.1)
//-----------------------------------------------------------------------------//
public class Compressor
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public int SequenceNo { get; set; } 
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; }
    public AlarmStatus AlarmStatus { get; set; }

    //----------------------------------------------------------------------------------//
    // 통신 장애 경보. 센서값 기준 AlarmStatus와는 완전히 별도로 관리한다(사용자 결정) — 통신이
    // 불안정해서 짧게 끊겼다 붙었다 하는 것까지 매번 경보로 잡지 않도록, 끊긴 시각을 기록해두고
    // CompressorPollingService.CommunicationFailureAlarmDelay 이상 계속 끊긴 상태일 때만 경보로 본다.
    //----------------------------------------------------------------------------------//
    public DateTimeOffset? DisconnectedSince { get; set; }
    public bool HasCommunicationAlarm { get; set; }
}
