namespace HRMS.Modules.Logging.Models;

//-----------------------------------------------------------------------------//
// 이벤트 조회 응답 항목 하나. 실시간 피드(GET /api/events)와 자료 조회
// (GET /api/equipments/{id}/events)가 같은 형태를 공유한다.
//
// EquipmentId/CompressorId/ChannelNo는 카테고리에 따라 null일 수 있다(예: 로그인 이벤트).
// Value/LowerLimit/UpperLimit/DecimalPlaces/Unit은 **경보(Alarm) 이벤트에서만** 채워지는
// 상세 스냅샷이며, 그 외 카테고리는 전부 null이다. 2026-09-16 이전에 쌓인 경보 이벤트도
// 소급 기록이 불가능해서 null이다 — 프론트는 값이 없는 경우를 처리해야 한다.
// Value/LowerLimit/UpperLimit은 채널값과 같은 raw 스케일이고, DecimalPlaces가 표시 변환 힌트다.
//-----------------------------------------------------------------------------//
public record EventLogDto(
    int Id,
    string Category,
    string Message,
    string? Username,
    int? EquipmentId,
    int? CompressorId,
    string? ChannelNo,
    short? Value,
    short? LowerLimit,
    short? UpperLimit,
    int? DecimalPlaces,
    string? Unit,
    DateTimeOffset CreatedAt);

// 실시간 피드와 자료 조회가 같은 변환을 쓰도록 매핑을 한 곳에 둔다(필드가 13개라 양쪽에
// 따로 두면 한쪽만 고치고 지나가기 쉽다).
public static class EventLogMapping
{
    public static EventLogDto ToDto(this EventLog e) => new(
        e.Id, e.Category.ToString(), e.Message, e.Username,
        e.EquipmentId, e.CompressorId, e.ChannelNo?.ToString(),
        e.Value, e.LowerLimit, e.UpperLimit, e.DecimalPlaces, e.Unit,
        e.CreatedAt);
}
