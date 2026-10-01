using HRMS.Modules.Equipment;
using HRMS.Modules.Auth;
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
    private static readonly ApprovalLevelModes Modes = ApprovalDocuments.ModesFor(ApprovalDocumentType.OperationLog);

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

        //--------------------------------------------------------------------------------//
        // 그 장비의 담당자(UserEquipment) 또는 시스템관리자만 저장할 수 있다(2026-09-28 추가).
        // 점검일지와 같은 기준 — 이 PUT도 전체 교체라 남의 장비 일지를 통째로 날릴 수 있었다.
        //--------------------------------------------------------------------------------//
        if (!await EquipmentAccess.CanManageAsync(db, User, request.EquipmentId))
            return Forbid();

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
        var log = await FindWithValuesAsync(id);
        if (log is null)
            return NotFound();

        if (!TryGetCurrentUser(out var userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        if (!await db.UserEquipments.AnyAsync(ue => ue.UserId == userId && ue.EquipmentId == log.EquipmentId))
            return Forbid();

        if (!ApprovalLevels.CanApprove(log, Modes, level))
            return Conflict("지금은 이 단계를 승인할 수 없습니다(이미 승인됨, 또는 이전 단계 미완료).");

        var user = await db.Users.FindAsync(userId);
        ApprovalLevels.Set(log, level, userId, user!.FullName, DateTimeOffset.UtcNow);
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
        var log = await FindWithValuesAsync(id);
        if (log is null)
            return NotFound();

        if (!TryGetCurrentUser(out var userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        var (approverId, approverName, _) = ApprovalLevels.Get(log, level);
        if (approverId != userId)
            return Forbid();

        if (!ApprovalLevels.CanCancel(log, Modes, level))
            return Conflict("상위 단계가 이미 승인되어 취소할 수 없습니다.");

        ApprovalLevels.Set(log, level, null, null, null);
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
        var log = await FindWithValuesAsync(id);
        if (log is null)
            return NotFound();

        ApprovalLevels.Reset(log);
        await db.SaveChangesAsync();

        await EventLogger.LogAsync(db, EventLogCategory.Approval,
            $"{User.Identity?.Name}님이 운전일지(장비ID {log.EquipmentId}, {log.Date}) 결재를 전부 초기화했습니다.",
            User.Identity?.Name, log.EquipmentId);

        return Ok(ToDto(log, log.EquipmentId, log.Date));
    }

    private bool TryGetCurrentUser(out int userId, out UserRole role) => User.TryGetUser(out userId, out role);

    //--------------------------------------------------------------------------------//
    // 결재·결재취소·결재초기화도 응답으로 문서 전체(ToDto)를 돌려주므로, 조회(GET)와 똑같이
    // 입력값(Items)과 참고값(References)을 함께 읽어야 한다. FindAsync만 쓰던 때는 결재 응답의
    // 값이 전부 비어 왔다(2026-09-29 점검일지에서 제보된 것과 같은 버그 — DB 값은 멀쩡했다).
    //--------------------------------------------------------------------------------//
    private Task<OperationLog?> FindWithValuesAsync(int id) =>
        db.OperationLogs
            .Include(l => l.Items)
            .Include(l => l.References)
            .FirstOrDefaultAsync(l => l.Id == id);

    private static OperationLogDto ToDto(OperationLog? log, int equipmentId, DateOnly date) => new(
        log?.Id, equipmentId, date,
        ApprovalRules.ToDto(Modes[1], log?.Level1ApproverName, log?.Level1ApprovedAt),
        ApprovalRules.ToDto(Modes[2], log?.Level2ApproverName, log?.Level2ApprovedAt),
        ApprovalRules.ToDto(Modes[3], log?.Level3ApproverName, log?.Level3ApprovedAt),
        (log?.Items ?? []).Select(i => new OperationItemDto(i.ItemKey, i.CompressorId, i.Time0900, i.Time1300, i.Time1600, i.Time2100)).ToList(),
        (log?.References ?? []).Select(r => new OperationReferenceDto(r.ItemKey, r.ReferenceText)).ToList());
}
