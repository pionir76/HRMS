namespace HRMS.Modules.Logging.Models;

// GET /api/events 응답 항목 하나. EquipmentId/CompressorId/ChannelNo는 카테고리에 따라 null일 수 있다.
public record EventLogDto(
    int Id,
    string Category,
    string Message,
    string? Username,
    int? EquipmentId,
    int? CompressorId,
    string? ChannelNo,
    DateTimeOffset CreatedAt);
