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
    // 장비 상태가 `운영`인 장비(와 그 압축기)만 센다 — 운영이 아닌 장비는 실시간 현황에
    // 노출하지 않는다(사용자 결정 2026-09-17).
    [HttpGet]
    public async Task<ActionResult<SystemSummaryDto>> GetSummary()
    {
        var equipments = db.Equipments.Where(e => e.Status == EquipmentStatus.운영);
        var compressors = db.Compressors.Where(c => equipments.Any(e => e.Id == c.EquipmentId));

        int totalEquipment = await equipments.CountAsync();
        int totalCompressor = await compressors.CountAsync();
        int runningEquipment = await equipments.CountAsync(e => e.IsRunning);
        int commFailedCompressor = await compressors.CountAsync(c => c.CommunicationStatus != CommunicationStatus.연결됨);

        return Ok(new SystemSummaryDto(totalEquipment, totalCompressor, runningEquipment, commFailedCompressor));
    }
}
