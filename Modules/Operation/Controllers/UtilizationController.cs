using HRMS.Infrastructure;
using HRMS.Modules.Operation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Operation.Controllers;

[ApiController]
[Route("api/equipments/{equipmentId}/utilization")]
[Authorize]
public class UtilizationController(AppDbContext db) : ControllerBase
{
    private static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

    // GET api/equipments/{id}/utilization?from=2026-08-01&to=2026-08-31
    // from/to는 한국 시간 기준 날짜(둘 다 포함)다. to 생략 시 from과 동일한 하루, 둘 다 생략 시 오늘.
    [HttpGet]
    public async Task<ActionResult<UtilizationDto>> GetUtilization(int equipmentId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (await db.Equipments.FindAsync(equipmentId) is null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(KstOffset).Date);
        var fromDate = from ?? today;
        var toDate = to ?? fromDate;

        // 압축기 여러 대를 가진 장비는 CompressorMeasurement에 같은 분·같은 RunningStatus가
        // 압축기 수만큼 중복 저장되어 있다(전부 장비의 RunningStatus를 그대로 복사한 값이라 비율은
        // 동일하지만 불필요하게 여러 번 읽게 됨) — 대표 압축기 1대(Id가 가장 작은 것)만 사용한다.
        var representativeCompressorId = await db.Compressors
            .Where(c => c.EquipmentId == equipmentId)
            .OrderBy(c => c.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        if (representativeCompressorId is not { } compressorId)
            return Ok(new UtilizationDto(equipmentId, fromDate, toDate, 0, 0, null));

        // 한국 시간 기준 날짜 범위를 UTC로 변환한다 (TrendController와 동일한 이유:
        // Npgsql은 timestamptz 비교 파라미터로 UTC(offset 0)만 받는다).
        var start = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), KstOffset).ToUniversalTime();
        var end = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MinValue), KstOffset).ToUniversalTime().AddDays(1);

        var totalMinutes = await db.CompressorMeasurements
            .Where(m => m.CompressorId == compressorId && m.MeasuredAt >= start && m.MeasuredAt < end)
            .CountAsync();

        // 요청한 기간 전체가 아니라 "실제 기록이 있는 분"을 분모로 쓴다 — 데이터 결측 구간(서버 다운 등)을
        // 미가동으로 몰아 넣지 않기 위함. 대신 TotalMinutes를 응답에 그대로 내려줘서, 프론트가
        // 그 비율이 실제로 며칠치 데이터를 근거로 한 건지 확인할 수 있게 한다.
        var runningMinutes = await db.CompressorMeasurements
            .Where(m => m.CompressorId == compressorId && m.MeasuredAt >= start && m.MeasuredAt < end
                && m.RunningStatus == RunningStatus.운전)
            .CountAsync();

        decimal? percent = totalMinutes == 0 ? null : Math.Round(runningMinutes * 100m / totalMinutes, 1);

        return Ok(new UtilizationDto(equipmentId, fromDate, toDate, totalMinutes, runningMinutes, percent));
    }
}
