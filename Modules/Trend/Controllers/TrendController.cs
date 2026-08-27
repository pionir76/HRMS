using HRMS.Infrastructure;
using HRMS.Modules.Trend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Trend.Controllers;

[ApiController]
[Route("api/compressors/{compressorId}/trend")]
[Authorize]
public class TrendController(AppDbContext db) : ControllerBase
{
    private static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

    //--------------------------------------------------------------------------------//
    // GET api/compressors/{id}/trend?date=2026-08-26 — date 생략 시 오늘(한국 시간) 기준.
    // 압축기 하루치 트렌드를 시간순으로 반환한다 (채널 전체 + 상태 스냅샷 포함).
    //--------------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<List<TrendPointDto>>> GetTrend(int compressorId, [FromQuery] DateOnly? date)
    {
        if (await db.Compressors.FindAsync(compressorId) is null)
            return NotFound();

        var day = date ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(KstOffset).Date);

        //--------------------------------------------------------------------------------//
        // 한국 시간 기준 하루(day 00:00 ~ 다음날 00:00)를 UTC로 변환해서 조회한다.
        // Npgsql은 timestamptz 비교 파라미터도 UTC(offset 0)만 받으므로 ToUniversalTime()이 필요하다
        // (TrendRecordingService에서 저장할 때 겪었던 것과 같은 종류의 제약).
        //--------------------------------------------------------------------------------//
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), KstOffset).ToUniversalTime();
        var end = start.AddDays(1);

        var entities = await db.CompressorMeasurements
            .Where(m => m.CompressorId == compressorId && m.MeasuredAt >= start && m.MeasuredAt < end)
            .OrderBy(m => m.MeasuredAt)
            .ToListAsync();

        return Ok(entities.Select(m => new TrendPointDto(
            m.MeasuredAt, m.Ch01, m.Ch02, m.Ch03, m.Ch04, m.Ch05, m.Ch06, m.Ch07,
            m.IsRunning, m.HasAlarm, m.IsConnected)).ToList());
    }
}
