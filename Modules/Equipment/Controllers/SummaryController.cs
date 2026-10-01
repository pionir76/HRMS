using HRMS.Infrastructure;
using HRMS.Modules.Communication.Models;
using HRMS.Modules.Equipment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment.Controllers;

// 실시간 현황 화면 상단 카운트 전용 집계 API.
[ApiController]
[Route("api/summary")]
[Authorize]
public class SummaryController(AppDbContext db) : ControllerBase
{
    // GET api/summary — 장비/압축기 수, 운전 중인 장비 수, 통신불량 압축기 수.
    // "운전 중인 압축기 수"는 없다 — 운전 여부는 장비 단위로만 판정한다(사용자 결정).
    // Total*Count는 장비 상태가 `운영`인 장비(와 그 압축기)만 센다 — 운영이 아닌 장비는 실시간
    // 현황에 노출하지 않는다(사용자 결정 2026-09-17). Registered*Count와 상태별 집계는 등록된
    // 전체 기준이다 — 프론트가 "운영 149대 / 미운영 9대"를 한 번의 호출로 표시하기 위함이다.
    [HttpGet]
    public async Task<ActionResult<SystemSummaryDto>> GetSummary()
    {
        var operational = db.Equipments.Where(e => e.Status == EquipmentStatus.운영);
        var operationalCompressors = db.Compressors.Where(c => operational.Any(e => e.Id == c.EquipmentId));

        int totalEquipment = await operational.CountAsync();
        int totalCompressor = await operationalCompressors.CountAsync();
        int runningEquipment = await operational.CountAsync(e => e.IsRunning);
        int commFailedCompressor = await operationalCompressors.CountAsync(c => c.CommunicationStatus != CommunicationStatus.연결됨);

        int registeredEquipment = await db.Equipments.CountAsync();
        int registeredCompressor = await db.Compressors.CountAsync();

        //---------------------------------------------------------------------------//
        // 상태별 장비 수. DB에 한 건도 없는 상태까지 0으로 채워서 EquipmentStatus 8개를 enum
        // 정의 순서대로 항상 내려준다(응답 모양을 고정 — Dtos.cs의 SystemSummaryDto 주석 참고).
        //---------------------------------------------------------------------------//
        var counted = await db.Equipments
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var countByStatus = Enum.GetValues<EquipmentStatus>()
            .ToDictionary(status => status.ToString(), status => counted.GetValueOrDefault(status));

        return Ok(new SystemSummaryDto(
            totalEquipment, registeredEquipment, countByStatus,
            totalCompressor, registeredCompressor,
            runningEquipment, commFailedCompressor));
    }
}
