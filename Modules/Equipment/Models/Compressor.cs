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

    //----------------------------------------------------------------------------------//
    // 이 압축기 TLC의 비상정지 상태. 폴링이 매 사이클 센서 7채널과 함께 읽는 D1805(DO 출력)가
    // 0이 아니면 true다(Doc/pclink protocol.md 4장). 명령을 보낸 쪽이 아니라 장비가 실제로
    // 알려주는 상태이므로, 현장 비상정지 버튼을 사람이 직접 눌렀을 때도 여기에 반영된다.
    //
    // **장비 판정에는 1번 압축기 값만 쓴다**(Equipment.IsEmergencyStopped). 2번 이후 압축기의
    // 값은 각 TLC가 보고한 원자료로 저장만 되고, 화면·판정 어디에도 쓰이지 않는다.
    //
    // 통신이 끊기면 갱신하지 않고 마지막 값을 유지한다 — 알 수 없는 것을 "해제됨"으로 보이게
    // 하는 쪽이 더 위험하다(IsRunning이 통신 두절 시 정지로 보는 것과 반대 방향의 판단이다).
    //----------------------------------------------------------------------------------//
    public bool IsEmergencyStopped { get; set; }

    //----------------------------------------------------------------------------------//
    // 압축기 등록(장비 등록 시점) 때 CH01~07 채널 설정 7행을 한 번에 같이 만들기 위한 탐색 속성.
    // ChannelDefaults.CreateAll() 참고.
    //----------------------------------------------------------------------------------//
    public List<CompressorChannelSetting> ChannelSettings { get; set; } = [];
}
