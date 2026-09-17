using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Approval;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using HRMS.Modules.OperationReport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.OperationReport.Controllers;

//--------------------------------------------------------------------------------//
// 운전일지 조회/저장/결재 API. 결재 3단계는 점검일지와 동일하게 전부 Required로 고정되어 있다.
// 점검일지와의 가장 큰 차이: 결재가 진행 중이어도 PUT(저장)을 막지 않는다 — 결재 완료 후 수정
// 잠금은 프론트가 UI로만 처리하기로 했다(Modules/OperationReport/README.md "공통 규칙" 참고).
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/operation-logs")]
[Authorize]
public class OperationLogsController(AppDbContext db) : ControllerBase
{
    private const ApprovalLevelMode Level1Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level2Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level3Mode = ApprovalLevelMode.Required;

    //--------------------------------------------------------------------------------//
    // GET api/operation-logs?equipmentId=1&date=2026-11-05 — 저장된 적 없는 날짜면
    // Id가 null인 빈 폼을 반환한다(404 아님). 정상 동작 중이면 자동 기록 서비스가 매일
    // 생성하므로 오늘/과거 날짜는 거의 항상 실제 데이터가 있어야 한다.
    //--------------------------------------------------------------------------------//
    [HttpGet]
    public async Task<ActionResult<OperationLogDto>> Get([FromQuery] int equipmentId, [FromQuery] DateOnly date)
    {
        if (await db.Equipments.FindAsync(equipmentId) is null)
            return NotFound();

        var log = await db.OperationLogs
            .Include(l => l.Items)
            .Include(l => l.References)
            .FirstOrDefaultAsync(l => l.EquipmentId == equipmentId && l.Date == date);

        return Ok(ToDto(log, equipmentId, date));
    }

    //--------------------------------------------------------------------------------//
    // PUT api/operation-logs — 저장(신규 작성/수정 겸용, (EquipmentId, Date) 기준 upsert).
    // 점검일지와 달리 결재가 진행 중이어도 저장을 막지 않는다(README 참고).
    //--------------------------------------------------------------------------------//
    [HttpPut]
    public async Task<ActionResult<OperationLogDto>> Save(SaveOperationLogRequest request)
    {
        if (await db.Equipments.FindAsync(request.EquipmentId) is null)
            return NotFound();

        var log = await db.OperationLogs
            .Include(l => l.Items)
            .Include(l => l.References)
            .FirstOrDefaultAsync(l => l.EquipmentId == request.EquipmentId && l.Date == request.Date);

        var now = DateTimeOffset.UtcNow;
        if (log is null)
        {
            log = new OperationLog { EquipmentId = request.EquipmentId, Date = request.Date, CreatedAt = now };
            db.OperationLogs.Add(log);
        }
        else
        {
            db.OperationItemValues.RemoveRange(log.Items);
            db.OperationReferenceValues.RemoveRange(log.References);
        }

        log.UpdatedAt = now;
        log.Items = request.Items.Select(i => new OperationItemValue
        {
            ItemKey = i.ItemKey, CompressorId = i.CompressorId,
            Time0900 = i.Time0900, Time1300 = i.Time1300, Time1600 = i.Time1600, Time2100 = i.Time2100
        }).ToList();
        log.References = request.References.Select(r => new OperationReferenceValue
        {
            ItemKey = r.ItemKey, ReferenceText = r.ReferenceText
        }).ToList();

        await db.SaveChangesAsync();
        return Ok(ToDto(log, request.EquipmentId, request.Date));
    }

    //--------------------------------------------------------------------------------//
    // POST api/operation-logs/{id}/approve — 호출자 본인의 Role+담당장비로 결재 레벨이 자동 결정된다.
    // 판정 로직은 점검일지와 동일하게 Modules/Approval을 그대로 재사용한다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<OperationLogDto>> Approve(int id)
    {
        var log = await db.OperationLogs.FindAsync(id);
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
            $"{user.FullName}({user.Role})님이 운전일지(장비ID {log.EquipmentId}, {log.Date})를 결재했습니다.",
            user.Username, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.Date));
    }

    //--------------------------------------------------------------------------------//
    // POST api/operation-logs/{id}/approve/cancel — 본인이 승인한 단계만, 상위 단계가
    // 아직 승인되지 않았을 때만 취소 가능하다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve/cancel")]
    public async Task<ActionResult<OperationLogDto>> CancelApprove(int id)
    {
        var log = await db.OperationLogs.FindAsync(id);
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
            $"{approverName}({role})님이 운전일지(장비ID {log.EquipmentId}, {log.Date}) 결재를 취소했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.Date));
    }

    //--------------------------------------------------------------------------------//
    // POST api/operation-logs/{id}/approvals/reset — 시스템관리자 전용. 진행 단계와 상관없이
    // 결재를 전부 무효화해서 관련자들이 다시 결재하게 만든다.
    //--------------------------------------------------------------------------------//
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/approvals/reset")]
    public async Task<ActionResult<OperationLogDto>> ResetApprovals(int id)
    {
        var log = await db.OperationLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        log.Level1ApproverId = null; log.Level1ApproverName = null; log.Level1ApprovedAt = null;
        log.Level2ApproverId = null; log.Level2ApproverName = null; log.Level2ApprovedAt = null;
        log.Level3ApproverId = null; log.Level3ApproverName = null; log.Level3ApprovedAt = null;
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{User.Identity?.Name}님이 운전일지(장비ID {log.EquipmentId}, {log.Date}) 결재를 전부 초기화했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.Date));
    }

    private bool TryGetCurrentUser(out int userId, out UserRole role)
    {
        role = default;
        userId = 0;
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (idClaim is null || roleClaim is null || !int.TryParse(idClaim, out userId) || !Enum.TryParse(roleClaim, out role))
            return false;
        return true;
    }

    private static (ApprovalLevelMode Mode, int? ApproverId, string? ApproverName, DateTimeOffset? ApprovedAt) GetLevel(OperationLog log, int level) => level switch
    {
        1 => (Level1Mode, log.Level1ApproverId, log.Level1ApproverName, log.Level1ApprovedAt),
        2 => (Level2Mode, log.Level2ApproverId, log.Level2ApproverName, log.Level2ApprovedAt),
        3 => (Level3Mode, log.Level3ApproverId, log.Level3ApproverName, log.Level3ApprovedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    private static bool IsLevelSatisfied(OperationLog log, int level)
    {
        var (mode, _, _, approvedAt) = GetLevel(log, level);
        return ApprovalRules.IsSatisfied(mode, approvedAt);
    }

    private static void SetLevel(OperationLog log, int level, int? approverId, string? approverName, DateTimeOffset? approvedAt)
    {
        switch (level)
        {
            case 1: log.Level1ApproverId = approverId; log.Level1ApproverName = approverName; log.Level1ApprovedAt = approvedAt; break;
            case 2: log.Level2ApproverId = approverId; log.Level2ApproverName = approverName; log.Level2ApprovedAt = approvedAt; break;
            case 3: log.Level3ApproverId = approverId; log.Level3ApproverName = approverName; log.Level3ApprovedAt = approvedAt; break;
        }
    }

    private static OperationLogDto ToDto(OperationLog? log, int equipmentId, DateOnly date) => new(
        log?.Id, equipmentId, date,
        ApprovalRules.ToDto(Level1Mode, log?.Level1ApproverName, log?.Level1ApprovedAt),
        ApprovalRules.ToDto(Level2Mode, log?.Level2ApproverName, log?.Level2ApprovedAt),
        ApprovalRules.ToDto(Level3Mode, log?.Level3ApproverName, log?.Level3ApprovedAt),
        (log?.Items ?? []).Select(i => new OperationItemDto(i.ItemKey, i.CompressorId, i.Time0900, i.Time1300, i.Time1600, i.Time2100)).ToList(),
        (log?.References ?? []).Select(r => new OperationReferenceDto(r.ItemKey, r.ReferenceText)).ToList());
}
