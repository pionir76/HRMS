using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Approval;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using HRMS.Modules.RepairLog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.RepairLog.Controllers;

//--------------------------------------------------------------------------------//
// 수리일지 조회/등록/수정/삭제/결재 API. 결재 판정은 Modules/Approval/ApprovalRules.cs를
// 점검일지와 동일하게 재사용하고, 이 문서유형만의 고정 사양은 아래 Level1~3Mode 상수다:
//   Level1(안전관리원) = NotApplicable("해당없음", 프론트 `/` 표시) → 실제 승인은 2단계
//   Level2(안전관리책임자) / Level3(안전관리총괄자) = Required
//
// 권한 규칙(사용자 결정):
//   - 조회/등록: 로그인한 사용자 누구나
//   - 수정: 결재가 하나도 진행되지 않았을 때, 시스템관리자 또는 작성자만
//   - 삭제: 결재 진행 전이면 시스템관리자 또는 작성자, 결재가 진행된 뒤에는 시스템관리자만
//   - 결재 진행 후에는 수정이 아예 불가능하다(409) — 승인한 사람이 본 내용이 나중에 바뀌는
//     것을 막기 위함이며, 점검일지의 "결재 중 수정 금지"와 같은 원칙이다.
//
// 첨부파일은 이 문서유형에서 지원하지 않는다(사용자 결정 — Modules/RepairLog/README.md 참고).
//
// PerformedAt은 저장 전에 반드시 ToUniversalTime()으로 정규화한다 — Npgsql은 timestamptz
// 컬럼에 offset이 0(UTC)인 DateTimeOffset만 허용해서, 프론트가 한국시간 오프셋(+09:00)으로
// 보내면 그냥 쓰면 500이 난다(TrendRecordingService에도 같은 제약이 주석으로 남아있다).
// 시점 자체는 그대로 보존되고 표현만 UTC로 바뀌며, 응답도 UTC로 내려간다(다른 API의
// measuredAt 등과 동일한 규칙 — 표시용 변환은 프론트 담당).
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/repair-logs")]
[Authorize]
public class RepairLogsController(AppDbContext db) : ControllerBase
{
    private const ApprovalLevelMode Level1Mode = ApprovalLevelMode.NotApplicable;
    private const ApprovalLevelMode Level2Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level3Mode = ApprovalLevelMode.Required;

    // GET api/repair-logs?equipmentId= — 해당 장비의 수리일지 목록, 일시 내림차순(최신 먼저)
    [HttpGet]
    public async Task<ActionResult<List<RepairLogDto>>> GetList([FromQuery] int equipmentId)
    {
        var logs = await db.RepairLogs
            .Where(l => l.EquipmentId == equipmentId)
            .OrderByDescending(l => l.PerformedAt)
            .ToListAsync();

        return Ok(logs.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RepairLogDto>> GetOne(int id)
    {
        var log = await db.RepairLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        return Ok(ToDto(log));
    }

    [HttpPost]
    public async Task<ActionResult<RepairLogDto>> Create(CreateRepairLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");
        if (await db.Equipments.FindAsync(request.EquipmentId) is null)
            return NotFound("존재하지 않는 장비입니다.");

        var (userId, userName) = await CurrentUserAsync();

        var log = new Models.RepairLog
        {
            EquipmentId = request.EquipmentId,
            Title = request.Title,
            PerformedAt = request.PerformedAt.ToUniversalTime(), // 아래 ToUniversalTime 주석 참고
            Location = request.Location,
            PerformedBy = request.PerformedBy,
            Target = request.Target,
            StateBefore = request.StateBefore,
            StateAfter = request.StateAfter,
            Result = request.Result,
            FailureCause = request.FailureCause,
            PreventiveAction = request.PreventiveAction,
            Opinion = request.Opinion,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.RepairLogs.Add(log);
        await db.SaveChangesAsync();
        return Ok(ToDto(log));
    }

    //--------------------------------------------------------------------------------//
    // PUT api/repair-logs/{id} — 결재가 하나라도 진행되면 수정 자체가 막힌다(409).
    // EquipmentId는 생성 후 고정이라 요청에 없다.
    //--------------------------------------------------------------------------------//
    [HttpPut("{id}")]
    public async Task<ActionResult<RepairLogDto>> Update(int id, UpdateRepairLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");

        var log = await db.RepairLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (HasAnyApproval(log))
            return Conflict("결재가 진행된 수리일지는 수정할 수 없습니다.");

        var (userId, userName) = await CurrentUserAsync();
        if (!IsSystemAdmin && log.CreatedByUserId != userId)
            return Forbid();

        log.Title = request.Title;
        log.PerformedAt = request.PerformedAt.ToUniversalTime();
        log.Location = request.Location;
        log.PerformedBy = request.PerformedBy;
        log.Target = request.Target;
        log.StateBefore = request.StateBefore;
        log.StateAfter = request.StateAfter;
        log.Result = request.Result;
        log.FailureCause = request.FailureCause;
        log.PreventiveAction = request.PreventiveAction;
        log.Opinion = request.Opinion;
        log.UpdatedByUserId = userId;
        log.UpdatedByUserName = userName;
        log.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(log));
    }

    //--------------------------------------------------------------------------------//
    // DELETE api/repair-logs/{id} — 결재 진행 전이면 시스템관리자 또는 작성자,
    // 결재가 진행된 뒤에는 시스템관리자만 삭제할 수 있다.
    //--------------------------------------------------------------------------------//
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await db.RepairLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (!CanDelete(log))
            return Forbid();

        db.RepairLogs.Remove(log);
        await db.SaveChangesAsync();
        return NoContent();
    }

    //--------------------------------------------------------------------------------//
    // POST api/repair-logs/{id}/approve — 호출자 본인의 Role+담당장비로 결재 레벨이 자동 결정된다.
    // 안전관리원은 이 문서유형에서 결재 대상이 아니라(NotApplicable) 승인할 수 없다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<RepairLogDto>> Approve(int id)
    {
        var log = await db.RepairLogs.FindAsync(id);
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
            return Conflict("지금은 이 단계를 승인할 수 없습니다(결재 대상이 아닌 단계이거나, 이미 승인됨, 또는 이전 단계 미완료).");

        var user = await db.Users.FindAsync(userId);
        SetLevel(log, level, userId, user!.FullName, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{user.FullName}({user.Role})님이 수리일지(장비ID {log.EquipmentId}, {log.Title})를 결재했습니다.",
            user.Username, log.EquipmentId);

        return Ok(ToDto(log));
    }

    //--------------------------------------------------------------------------------//
    // POST api/repair-logs/{id}/approve/cancel — 본인이 승인한 단계만, 상위 단계가 아직
    // 승인되지 않았을 때만 취소 가능하다.
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve/cancel")]
    public async Task<ActionResult<RepairLogDto>> CancelApprove(int id)
    {
        var log = await db.RepairLogs.FindAsync(id);
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
            $"{approverName}({role})님이 수리일지(장비ID {log.EquipmentId}, {log.Title}) 결재를 취소했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log));
    }

    //--------------------------------------------------------------------------------//
    // POST api/repair-logs/{id}/approvals/reset — 시스템관리자 전용. 결재를 전부 무효화한다.
    // 리셋하면 다시 수정 가능한 상태로 돌아간다(결재 진행 여부로 수정 잠금을 판단하므로).
    //--------------------------------------------------------------------------------//
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/approvals/reset")]
    public async Task<ActionResult<RepairLogDto>> ResetApprovals(int id)
    {
        var log = await db.RepairLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        log.Level1ApproverId = null; log.Level1ApproverName = null; log.Level1ApprovedAt = null;
        log.Level2ApproverId = null; log.Level2ApproverName = null; log.Level2ApprovedAt = null;
        log.Level3ApproverId = null; log.Level3ApproverName = null; log.Level3ApprovedAt = null;
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{User.Identity?.Name}님이 수리일지(장비ID {log.EquipmentId}, {log.Title}) 결재를 전부 초기화했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log));
    }

    // 결재가 한 단계라도 실제로 승인됐는지. Level1은 NotApplicable이라 채워지지 않지만,
    // 모드가 바뀌어도 그대로 동작하도록 세 레벨 전부 본다.
    private static bool HasAnyApproval(Models.RepairLog log) =>
        log.Level1ApprovedAt is not null || log.Level2ApprovedAt is not null || log.Level3ApprovedAt is not null;

    private bool IsSystemAdmin => User.IsSystemAdmin();

    private int CurrentUserId => User.GetUserId();

    private bool CanEdit(Models.RepairLog log) =>
        !HasAnyApproval(log) && (IsSystemAdmin || log.CreatedByUserId == CurrentUserId);

    private bool CanDelete(Models.RepairLog log) =>
        HasAnyApproval(log)
            ? IsSystemAdmin
            : IsSystemAdmin || log.CreatedByUserId == CurrentUserId;

    private async Task<(int UserId, string UserName)> CurrentUserAsync()
    {
        var userId = CurrentUserId;
        var userName = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync();
        return (userId, userName);
    }

    private bool TryGetCurrentUser(out int userId, out UserRole role) => User.TryGetUser(out userId, out role);

    private static (ApprovalLevelMode Mode, int? ApproverId, string? ApproverName, DateTimeOffset? ApprovedAt) GetLevel(Models.RepairLog log, int level) => level switch
    {
        1 => (Level1Mode, log.Level1ApproverId, log.Level1ApproverName, log.Level1ApprovedAt),
        2 => (Level2Mode, log.Level2ApproverId, log.Level2ApproverName, log.Level2ApprovedAt),
        3 => (Level3Mode, log.Level3ApproverId, log.Level3ApproverName, log.Level3ApprovedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    private static bool IsLevelSatisfied(Models.RepairLog log, int level)
    {
        var (mode, _, _, approvedAt) = GetLevel(log, level);
        return ApprovalRules.IsSatisfied(mode, approvedAt);
    }

    private static void SetLevel(Models.RepairLog log, int level, int? approverId, string? approverName, DateTimeOffset? approvedAt)
    {
        switch (level)
        {
            case 1: log.Level1ApproverId = approverId; log.Level1ApproverName = approverName; log.Level1ApprovedAt = approvedAt; break;
            case 2: log.Level2ApproverId = approverId; log.Level2ApproverName = approverName; log.Level2ApprovedAt = approvedAt; break;
            case 3: log.Level3ApproverId = approverId; log.Level3ApproverName = approverName; log.Level3ApprovedAt = approvedAt; break;
        }
    }

    // CanEdit/CanDelete가 요청자 기준 계산값이라 static이 아니다.
    private RepairLogDto ToDto(Models.RepairLog log) => new(
        log.Id, log.EquipmentId, log.Title, log.PerformedAt,
        log.Location, log.PerformedBy, log.Target,
        log.StateBefore, log.StateAfter, log.Result,
        log.FailureCause, log.PreventiveAction, log.Opinion,
        log.CreatedByUserName, log.CreatedAt, log.UpdatedByUserName, log.UpdatedAt,
        ApprovalRules.ToDto(Level1Mode, log.Level1ApproverName, log.Level1ApprovedAt),
        ApprovalRules.ToDto(Level2Mode, log.Level2ApproverName, log.Level2ApprovedAt),
        ApprovalRules.ToDto(Level3Mode, log.Level3ApproverName, log.Level3ApprovedAt),
        CanEdit(log), CanDelete(log));
}
