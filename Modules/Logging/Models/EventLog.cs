using HRMS.Modules.Equipment.Models;

namespace HRMS.Modules.Logging.Models;

//-----------------------------------------------------------------------------//
// 이벤트 로그. 시스템 전반에서 발생하는 이벤트를 기록한다. (로그인/로그아웃, 장비 통신 장애, 경보 발생 등)
// 이벤트가 어느 장비/압축기/채널에서 발생했는지(해당 없으면 null — 예: 로그인 이벤트).
// Message는 표시용 문장이고, 이 필드들은 프론트가 문자열 파싱 없이 필터링·네비게이션할 때 쓴다.
//-----------------------------------------------------------------------------//
public class EventLog
{
    public int Id { get; set; }
    public EventLogCategory Category { get; set; }
    public string Message { get; set; } = "";
    public string? Username { get; set; }

    public int? EquipmentId { get; set; }
    public int? CompressorId { get; set; }
    public ChannelNo? ChannelNo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
