using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Approval;
using HRMS.Modules.Attachment;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using HRMS.Modules.TrainingLog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.TrainingLog.Controllers;

//--------------------------------------------------------------------------------//
// 교육훈련 일지 조회/등록/수정/삭제/결재 API. 결재 판정은 Modules/Approval/ApprovalRules.cs를
// 다른 문서들과 동일하게 재사용하고, 이 문서유형의 고정 사양은 아래 Level1~3Mode 상수다:
//   Level1(안전관리원) = NotApplicable("해당없음", 프론트 `/` 표시) → 실제 승인은 2단계
//   Level2(안전관리책임자) / Level3(안전관리총괄자) = Required
//
// **다른 결재 문서와 다른 점**: 교육훈련은 장비에 매달리지 않는 전사 문서라 승인 시
// 담당 장비(UserEquipment)를 검사하지 않고 역할만 본다(사용자 결정). EventLog에도
// equipmentId를 남기지 않는다.
//
// 권한 규칙(수리일지와 동일 — 사용자 결정):
//   - 조회/등록: 로그인한 사용자 누구나
//   - 수정: 결재가 하나도 진행되지 않았을 때, 시스템관리자 또는 작성자만
//   - 삭제: 결재 진행 전이면 시스템관리자 또는 작성자, 결재가 진행된 뒤에는 시스템관리자만
//   - 결재가 한 단계라도 승인되면 수정 자체가 불가능하다(409). 첨부파일 추가/삭제도 같은
//     이유로 막힌다(AttachmentsController의 TrainingLog 분기에서 처리).
//
// PerformedAt은 저장 전에 ToUniversalTime()으로 정규화한다 — Npgsql이 timestamptz에
// offset 0(UTC)만 허용하기 때문이다(수리일지와 동일).
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/training-logs")]
[Authorize]
public class TrainingLogsController(AppDbContext db, AttachmentStorage storage) : ControllerBase
{
    private const ApprovalLevelMode Level1Mode = ApprovalLevelMode.NotApplicable;
    private const ApprovalLevelMode Level2Mode = ApprovalLevelMode.Required;
    private const ApprovalLevelMode Level3Mode = ApprovalLevelMode.Required;

    // GET api/training-logs — 전체 목록, 일시 내림차순(최신 먼저). 장비별 문서가 아니라 필터가 없다.
    [HttpGet]
    public async Task<ActionResult<List<TrainingLogDto>>> GetList()
    {
        var logs = await db.TrainingLogs
            .OrderByDescending(l => l.PerformedAt)
            .ToListAsync();

        var counts = await GetAttachmentCountsAsync(logs.Select(l => l.Id));
        return Ok(logs.Select(l => ToDto(l, counts.GetValueOrDefault(l.Id))).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TrainingLogDto>> GetOne(int id)
    {
        var log = await db.TrainingLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(log, counts.GetValueOrDefault(id)));
    }

    [HttpPost]
    public async Task<ActionResult<TrainingLogDto>> Create(SaveTrainingLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");

        var (userId, userName) = await CurrentUserAsync();

        var log = new Models.TrainingLog
        {
            Title = request.Title,
            PerformedAt = request.PerformedAt.ToUniversalTime(),
            Location = request.Location,
            Instructor = request.Instructor,
            Content = request.Content,
            Attendees = request.Attendees,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.TrainingLogs.Add(log);
        await db.SaveChangesAsync();
        return Ok(ToDto(log, 0));
    }

    // PUT api/training-logs/{id} — 결재가 하나라도 진행되면 수정 자체가 막힌다(409).
    [HttpPut("{id}")]
    public async Task<ActionResult<TrainingLogDto>> Update(int id, SaveTrainingLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");

        var log = await db.TrainingLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (HasAnyApproval(log))
            return Conflict("결재가 진행된 교육훈련 일지는 수정할 수 없습니다.");

        var (userId, userName) = await CurrentUserAsync();
        if (!IsSystemAdmin && log.CreatedByUserId != userId)
            return Forbid();

        log.Title = request.Title;
        log.PerformedAt = request.PerformedAt.ToUniversalTime();
        log.Location = request.Location;
        log.Instructor = request.Instructor;
        log.Content = request.Content;
        log.Attendees = request.Attendees;
        log.UpdatedByUserId = userId;
        log.UpdatedByUserName = userName;
        log.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(log, counts.GetValueOrDefault(id)));
    }

    //--------------------------------------------------------------------------------//
    // DELETE api/training-logs/{id} — 결재 진행 전이면 시스템관리자 또는 작성자,
    // 결재가 진행된 뒤에는 시스템관리자만. 딸린 첨부파일(파일+메타데이터)도 같이 정리한다.
    //--------------------------------------------------------------------------------//
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await db.TrainingLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (!CanDelete(log))
            return Forbid();

        var attachments = await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.TrainingLog && a.OwnerId == id)
            .ToListAsync();
        foreach (var a in attachments)
            storage.Delete(a.StoredPath);
        db.Attachments.RemoveRange(attachments);

        db.TrainingLogs.Remove(log);
        await db.SaveChangesAsync();
        return NoContent();
    }

    //--------------------------------------------------------------------------------//
    // POST api/training-logs/{id}/approve — 호출자 본인의 Role로 결재 레벨이 자동 결정된다.
    // 장비와 무관한 문서라 담당 장비 검사는 하지 않는다(다른 결재 문서와의 유일한 차이).
    //--------------------------------------------------------------------------------//
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<TrainingLogDto>> Approve(int id)
    {
        var log = await db.TrainingLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        if (!TryGetCurrentUser(out var userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        var (mode, _, _, approvedAt) = GetLevel(log, level);
        var previousSatisfied = level == 1 || IsLevelSatisfied(log, level - 1);
        if (!ApprovalRules.CanApprove(mode, approvedAt, previousSatisfied))
            return Conflict("지금은 이 단계를 승인할 수 없습니다(결재 대상이 아닌 단계이거나, 이미 승인됨, 또는 이전 단계 미완료).");

        var user = await db.Users.FindAsync(userId);
        SetLevel(log, level, userId, user!.FullName, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{user.FullName}({user.Role})님이 교육훈련 일지({log.Title})를 결재했습니다.",
            user.Username);

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(log, counts.GetValueOrDefault(id)));
    }

    // POST api/training-logs/{id}/approve/cancel — 본인이 승인한 단계만, 상위 단계가 아직
    // 승인되지 않았을 때만 취소 가능하다.
    [HttpPost("{id}/approve/cancel")]
    public async Task<ActionResult<TrainingLogDto>> CancelApprove(int id)
    {
        var log = await db.TrainingLogs.FindAsync(id);
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
            $"{approverName}({role})님이 교육훈련 일지({log.Title}) 결재를 취소했습니다.",
            User.Identity?.Name);

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(log, counts.GetValueOrDefault(id)));
    }

    // POST api/training-logs/{id}/approvals/reset — 시스템관리자 전용. 리셋하면 수정 잠금도 풀린다.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/approvals/reset")]
    public async Task<ActionResult<TrainingLogDto>> ResetApprovals(int id)
    {
        var log = await db.TrainingLogs.FindAsync(id);
        if (log is null)
            return NotFound();

        log.Level1ApproverId = null; log.Level1ApproverName = null; log.Level1ApprovedAt = null;
        log.Level2ApproverId = null; log.Level2ApproverName = null; log.Level2ApprovedAt = null;
        log.Level3ApproverId = null; log.Level3ApproverName = null; log.Level3ApprovedAt = null;
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{User.Identity?.Name}님이 교육훈련 일지({log.Title}) 결재를 전부 초기화했습니다.",
            User.Identity?.Name);

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(log, counts.GetValueOrDefault(id)));
    }

    private static bool HasAnyApproval(Models.TrainingLog log) =>
        log.Level1ApprovedAt is not null || log.Level2ApprovedAt is not null || log.Level3ApprovedAt is not null;

    private bool IsSystemAdmin =>
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role) && role == UserRole.시스템관리자;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool CanEdit(Models.TrainingLog log) =>
        !HasAnyApproval(log) && (IsSystemAdmin || log.CreatedByUserId == CurrentUserId);

    private bool CanDelete(Models.TrainingLog log) =>
        HasAnyApproval(log)
            ? IsSystemAdmin
            : IsSystemAdmin || log.CreatedByUserId == CurrentUserId;

    private async Task<(int UserId, string UserName)> CurrentUserAsync()
    {
        var userId = CurrentUserId;
        var userName = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync();
        return (userId, userName);
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

    private async Task<Dictionary<int, int>> GetAttachmentCountsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        return await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.TrainingLog && idList.Contains(a.OwnerId))
            .GroupBy(a => a.OwnerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    private static (ApprovalLevelMode Mode, int? ApproverId, string? ApproverName, DateTimeOffset? ApprovedAt) GetLevel(Models.TrainingLog log, int level) => level switch
    {
        1 => (Level1Mode, log.Level1ApproverId, log.Level1ApproverName, log.Level1ApprovedAt),
        2 => (Level2Mode, log.Level2ApproverId, log.Level2ApproverName, log.Level2ApprovedAt),
        3 => (Level3Mode, log.Level3ApproverId, log.Level3ApproverName, log.Level3ApprovedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    private static bool IsLevelSatisfied(Models.TrainingLog log, int level)
    {
        var (mode, _, _, approvedAt) = GetLevel(log, level);
        return ApprovalRules.IsSatisfied(mode, approvedAt);
    }

    private static void SetLevel(Models.TrainingLog log, int level, int? approverId, string? approverName, DateTimeOffset? approvedAt)
    {
        switch (level)
        {
            case 1: log.Level1ApproverId = approverId; log.Level1ApproverName = approverName; log.Level1ApprovedAt = approvedAt; break;
            case 2: log.Level2ApproverId = approverId; log.Level2ApproverName = approverName; log.Level2ApprovedAt = approvedAt; break;
            case 3: log.Level3ApproverId = approverId; log.Level3ApproverName = approverName; log.Level3ApprovedAt = approvedAt; break;
        }
    }

    // CanEdit/CanDelete가 요청자 기준 계산값이라 static이 아니다.
    private TrainingLogDto ToDto(Models.TrainingLog log, int attachmentCount) => new(
        log.Id, log.Title, log.PerformedAt, log.Location, log.Instructor, log.Content, log.Attendees,
        log.CreatedByUserName, log.CreatedAt, log.UpdatedByUserName, log.UpdatedAt,
        ApprovalRules.ToDto(Level1Mode, log.Level1ApproverName, log.Level1ApprovedAt),
        ApprovalRules.ToDto(Level2Mode, log.Level2ApproverName, log.Level2ApprovedAt),
        ApprovalRules.ToDto(Level3Mode, log.Level3ApproverName, log.Level3ApprovedAt),
        attachmentCount, CanEdit(log), CanDelete(log));
}
