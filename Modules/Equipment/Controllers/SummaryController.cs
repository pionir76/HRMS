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
    // GET api/summary — 전체 장비/압축기 수, 운전 중인 장비 수, 통신불량 압축기 수.
    // "운전 중인 압축기 수"는 없다 — 운전 여부는 장비 단위로만 판정한다(사용자 결정).
    [HttpGet]
    public async Task<ActionResult<SystemSummaryDto>> GetSummary()
    {
        int totalEquipment = await db.Equipments.CountAsync();
        int totalCompressor = await db.Compressors.CountAsync();
        int runningEquipment = await db.Equipments.CountAsync(e => e.IsRunning);
        int commFailedCompressor = await db.Compressors.CountAsync(c => c.CommunicationStatus != CommunicationStatus.연결됨);

        return Ok(new SystemSummaryDto(totalEquipment, totalCompressor, runningEquipment, commFailedCompressor));
    }
}
