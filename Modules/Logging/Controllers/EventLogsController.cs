using HRMS.Infrastructure;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Logging.Controllers;

// 이벤트 로그 조회 API. 실시간 현황 화면의 이벤트 피드용.
[ApiController]
[Route("api/events")]
[Authorize]
public class EventLogsController(AppDbContext db) : ControllerBase
{
    private const int DefaultTake = 100;
    private const int MaxTake = 500;

    // GET api/events?since=&take=
    // - since 생략: 최신 이벤트부터 최대 take개 (최초 조회용).
    // - since 지정: 그 시각 이후 이벤트를 오래된 순으로 최대 take개 (이어서 폴링할 때 누락 방지용 —
    //   최신순으로 자르면 폴링 사이에 몰린 이벤트 중 오래된 것들이 영영 안 보일 수 있다).
    [HttpGet]
    public async Task<ActionResult<List<EventLogDto>>> GetEvents([FromQuery] DateTimeOffset? since, [FromQuery] int? take)
    {
        int limit = Math.Clamp(take ?? DefaultTake, 1, MaxTake);

        var entities = since is { } s
            ? await db.EventLogs.Where(e => e.CreatedAt > s).OrderBy(e => e.CreatedAt).Take(limit).ToListAsync()
            : await db.EventLogs.OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync();

        return Ok(entities.Select(e => new EventLogDto(
            e.Id, e.Category.ToString(), e.Message, e.Username,
            e.EquipmentId, e.CompressorId, e.ChannelNo?.ToString(), e.CreatedAt)).ToList());
    }
}
