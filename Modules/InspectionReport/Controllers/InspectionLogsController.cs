using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Approval;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.InspectionReport.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.InspectionReport.Controllers;

//--------------------------------------------------------------------------------//
// 점검일지 조회/작성/결재 API. 결재 3단계는 전부 Required로 고정되어 있다(안전관리원/
// 안전관리책임자/안전관리총괄자 모두 참여) — 다른 문서유형에서 NotApplicable/Delegated를
// 쓰게 되면 이 상수만 바꾸면 된다. 판정 로직은 Modules/Approval/ApprovalRules.cs 공유.
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/inspection-logs")]
[Authorize]
public class InspectionLogsController(AppDbContext db) : ControllerBase
{
    private const ApprovalLevelMode Level1Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level2Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level3Mode = ApprovalLevelMode.Required;

    //--------------------------------------------------------------------------------//
    // GET api/inspection-logs?equipmentId=1&weekStart=2026-08-30
    // weekStart는 반드시 일요일 날짜. 저장된 적 없는 주차면 Id가 null인 빈 폼을 반환한다(404 아님).
    //--------------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<InspectionLogDto>> Get([FromQuery] int equipmentId, [FromQuery] DateOnly weekStart)
    {
        if (await db.Equipments.FindAsync(equipmentId) is null)
            return NotFound();

        if (weekStart.DayOfWeek != DayOfWeek.Sunday)
            return BadRequest("weekStart는 일요일 날짜여야 합니다.");

        var log = await db.InspectionLogs
            .Include(l => l.Results)
            .FirstOrDefaultAsync(l => l.EquipmentId == equipmentId && l.WeekStartDate == weekStart);

        return Ok(ToDto(log, equipmentId, weekStart));
    }

    //--------------------------------------------------------------------------------//
    // PUT api/inspection-logs — 저장(신규 작성/수정 겸용, (EquipmentId, WeekStartDate) 기준 upsert).
    // 결재가 하나라도 진행된 뒤에는 수정할 수 없다(409).
    //--------------------------------------------------------------------------------//
    [HttpPut]
    public async Task<ActionResult<InspectionLogDto>> Save(SaveInspectionLogRequest request)
    {
        if (await db.Equipments.FindAsync(request.EquipmentId) is null)
            return NotFound();

        if (request.WeekStartDate.DayOfWeek != DayOfWeek.Sunday)
            return BadRequest("weekStartDate는 일요일 날짜여야 합니다.");

        var validItemNos = InspectionItemCatalog.Items.Select(i => i.ItemNo).ToHashSet();
        foreach (var r in request.Results)
        {
            if (!validItemNos.Contains(r.ItemNo))
                return BadRequest($"알 수 없는 점검항목 번호: {r.ItemNo}");
            foreach (var v in new[] { r.Sun, r.Mon, r.Tue, r.Wed, r.Thu, r.Fri, r.Sat })
                if (v is not null && v != "O" && v != "/" && v != "X")
                    return BadRequest($"점검 결과 값은 O, /, X 중 하나여야 합니다: {v}");
        }

        var log = await db.InspectionLogs
            .Include(l => l.Results)
            .FirstOrDefaultAsync(l => l.EquipmentId == request.EquipmentId && l.WeekStartDate == request.WeekStartDate);

        if (log is not null && (log.Level1ApprovedAt is not null || log.Level2ApprovedAt is not null || log.Level3ApprovedAt is not null))
            return Conflict("결재가 진행 중인 점검일지는 수정할 수 없습니다.");

        var now = DateTimeOffset.UtcNow;
        if (log is null)
        {
            log = new InspectionLog { EquipmentId = request.EquipmentId, WeekStartDate = request.WeekStartDate, CreatedAt = now };
            db.InspectionLogs.Add(log);
        }
        else
        {
            db.InspectionResults.RemoveRange(log.Results);
        }

        log.Opinion = request.Opinion;
        log.UpdatedAt = now;
        log.Results = request.Results.Select(r => new InspectionResult
        {
            ItemNo = r.ItemNo, Sun = r.Sun, Mon = r.Mon, Tue = r.Tue, Wed = r.Wed, Thu = r.Thu, Fri = r.Fri, Sat = r.Sat, Memo = r.Memo
        }).ToList();

        await db.SaveChangesAsync();
        return Ok(ToDto(log, request.EquipmentId, request.WeekStartDate));
    }

    //--------------------------------------------------------------------------------//
    // POST api/inspection-logs/{id}/approve — 호출자 본인의 Role+담당장비로 결재 레벨이 자동 결정된다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<InspectionLogDto>> Approve(int id)
    {
        var log = await db.InspectionLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (!TryGetCurrentUser(out var userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        if (!await db.UserEquipments.AnyAsync(ue => ue.UserId == userId && ue.EquipmentId == log.EquipmentId))
            return Forbid();

        var (mode, _, _, approvedAt) = GetLevel(log, level);
        var previousSatisfied = level == 1 || IsLevelSatisfied(log, level - 1);
        if (!ApprovalRules.CanApprove(mode, approvedAt, previousSatisfied))
            return Conflict("지금은 이 단계를 승인할 수 없습니다(이미 승인됨, 또는 이전 단계 미완료).");

        var user = await db.Users.FindAsync(userId);
        SetLevel(log, level, userId, user!.FullName, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{user.FullName}({user.Role})님이 점검일지(장비ID {log.EquipmentId}, {log.WeekStartDate} 주)를 결재했습니다.",
            user.Username, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.WeekStartDate));
    }

    //--------------------------------------------------------------------------------//
    // POST api/inspection-logs/{id}/approve/cancel — 본인이 승인한 단계만, 상위 단계가
    // 아직 승인되지 않았을 때만 취소 가능하다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve/cancel")]
    public async Task<ActionResult<InspectionLogDto>> CancelApprove(int id)
    {
        var log = await db.InspectionLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (!TryGetCurrentUser(out var userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        var (_, approverId, approverName, approvedAt) = GetLevel(log, level);
        if (approverId != userId)
            return Forbid();

        var nextSatisfied = level < 3 && IsLevelSatisfied(log, level + 1);
        if (!ApprovalRules.CanCancel(approvedAt, nextSatisfied))
            return Conflict("상위 단계가 이미 승인되어 취소할 수 없습니다.");

        SetLevel(log, level, null, null, null);
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{approverName}({role})님이 점검일지(장비ID {log.EquipmentId}, {log.WeekStartDate} 주) 결재를 취소했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.WeekStartDate));
    }

    //--------------------------------------------------------------------------------//
    // POST api/inspection-logs/{id}/approvals/reset — 시스템관리자 전용. 진행 단계와 상관없이
    // 결재를 전부 무효화해서 관련자들이 다시 결재하게 만든다. 시스템관리자는 결재 자체는 못 한다.
    //--------------------------------------------------------------------------------//
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/approvals/reset")]
    public async Task<ActionResult<InspectionLogDto>> ResetApprovals(int id)
    {
        var log = await db.InspectionLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        log.Level1ApproverId = null; log.Level1ApproverName = null; log.Level1ApprovedAt = null;
        log.Level2ApproverId = null; log.Level2ApproverName = null; log.Level2ApprovedAt = null;
        log.Level3ApproverId = null; log.Level3ApproverName = null; log.Level3ApprovedAt = null;
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{User.Identity?.Name}님이 점검일지(장비ID {log.EquipmentId}, {log.WeekStartDate} 주) 결재를 전부 초기화했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.WeekStartDate));
    }

    private bool TryGetCurrentUser(out int userId, out UserRole role) => User.TryGetUser(out userId, out role);

    private static (ApprovalLevelMode Mode, int? ApproverId, string? ApproverName, DateTimeOffset? ApprovedAt) GetLevel(InspectionLog log, int level) => level switch
    {
        1 => (Level1Mode, log.Level1ApproverId, log.Level1ApproverName, log.Level1ApprovedAt),
        2 => (Level2Mode, log.Level2ApproverId, log.Level2ApproverName, log.Level2ApprovedAt),
        3 => (Level3Mode, log.Level3ApproverId, log.Level3ApproverName, log.Level3ApprovedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    private static bool IsLevelSatisfied(InspectionLog log, int level)
    {
        var (mode, _, _, approvedAt) = GetLevel(log, level);
        return ApprovalRules.IsSatisfied(mode, approvedAt);
    }

    private static void SetLevel(InspectionLog log, int level, int? approverId, string? approverName, DateTimeOffset? approvedAt)
    {
        switch (level)
        {
            case 1: log.Level1ApproverId = approverId; log.Level1ApproverName = approverName; log.Level1ApprovedAt = approvedAt; break;
            case 2: log.Level2ApproverId = approverId; log.Level2ApproverName = approverName; log.Level2ApprovedAt = approvedAt; break;
            case 3: log.Level3ApproverId = approverId; log.Level3ApproverName = approverName; log.Level3ApprovedAt = approvedAt; break;
        }
    }

    private static InspectionLogDto ToDto(InspectionLog? log, int equipmentId, DateOnly weekStart)
    {
        var resultsByItem = (log?.Results ?? []).ToDictionary(r => r.ItemNo);
        var results = InspectionItemCatalog.Items.Select(item =>
        {
            resultsByItem.TryGetValue(item.ItemNo, out var r);
            return new InspectionResultDto(item.ItemNo, item.Category, item.Content,
                r?.Sun, r?.Mon, r?.Tue, r?.Wed, r?.Thu, r?.Fri, r?.Sat, r?.Memo);
        }).ToList();

        return new InspectionLogDto(
            log?.Id, equipmentId, weekStart, log?.Opinion,
            ApprovalRules.ToDto(Level1Mode, log?.Level1ApproverName, log?.Level1ApprovedAt),
            ApprovalRules.ToDto(Level2Mode, log?.Level2ApproverName, log?.Level2ApprovedAt),
            ApprovalRules.ToDto(Level3Mode, log?.Level3ApproverName, log?.Level3ApprovedAt),
            results);
    }
}
