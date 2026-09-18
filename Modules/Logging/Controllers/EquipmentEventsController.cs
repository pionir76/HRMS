using HRMS.Common;
using HRMS.Infrastructure;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Logging.Controllers;

//-----------------------------------------------------------------------------//
// 자료 조회 화면용 이벤트 조회 API. 실시간 현황 화면의 이벤트 피드(EventLogsController,
// GET /api/events)와는 목적이 달라서 엔드포인트를 따로 둔다:
//   - 실시간 피드: since/take 기반 폴링(누락 없이 이어받기), 최대 500건
//   - 자료 조회: 특정 장비 + 특정 날짜의 이벤트를 하루치 전부(페이징 없음)
//
// 경로는 장비 하위지만 도메인이 로깅이라 컨트롤러는 Modules/Logging에 둔다
// (Modules/Operation의 UtilizationController가 /api/equipments/{id}/utilization을
// 담당하는 것과 같은 방식).
//-----------------------------------------------------------------------------//
[ApiController]
[Route("api/equipments/{equipmentId}/events")]
[Authorize]
public class EquipmentEventsController(AppDbContext db) : ControllerBase
{

    //-----------------------------------------------------------------------------//
    // GET api/equipments/{id}/events?date=2026-09-16&category=Alarm
    // - date 생략 시 오늘(한국 시간) 기준 하루.
    // - category 생략 시 전체. 지정하면 그 카테고리만(예: Alarm, Communication).
    // 하루치를 전부 반환한다(페이징 없음, 사용자 결정) — 장비 1대의 하루 이벤트가 수천 건까지
    // 나올 수 있어서, 양이 부담되면 category 필터로 줄여 쓰는 것을 전제로 한다.
    //-----------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<List<EventLogDto>>> GetEquipmentEvents(
        int equipmentId, [FromQuery] DateOnly? date, [FromQuery] string? category)
    {
        if (await db.Equipments.FindAsync(equipmentId) is null)
            return NotFound();

        EventLogCategory? categoryFilter = null;
        if (category is not null)
        {
            if (!Enum.TryParse<EventLogCategory>(category, out var parsed))
                return BadRequest("올바르지 않은 category입니다.");
            categoryFilter = parsed;
        }

        // 한국 시간 기준 하루를 UTC 범위로 바꿔서 조회한다 — Npgsql은 timestamptz 비교
        // 파라미터도 UTC(offset 0)만 받는다(TrendController/UtilizationController와 동일).
        var day = date ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(KoreanTime.Offset).Date);
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), KoreanTime.Offset).ToUniversalTime();
        var end = start.AddDays(1);

        var query = db.EventLogs.Where(e => e.EquipmentId == equipmentId && e.CreatedAt >= start && e.CreatedAt < end);
        if (categoryFilter is { } c)
            query = query.Where(e => e.Category == c);

        var entities = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return Ok(entities.Select(e => e.ToDto()).ToList());
    }
}
