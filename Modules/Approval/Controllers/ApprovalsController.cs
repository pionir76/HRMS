using HRMS.Common;
using HRMS.Infrastructure;
using HRMS.Modules.Auth;
using HRMS.Modules.InspectionReport.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using HRMS.Modules.OperationReport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Approval.Controllers;

//--------------------------------------------------------------------------------//
// 일괄 결재 API(2026-09-28 추가). 담당 장비가 많은 사용자가 결재할 문서를 화면마다 일일이
// 찾아 들어가는 수고를 없애기 위한 기능이다(상단바 결재 배지 + 일괄 결재 팝업).
//
// 스키마는 전혀 건드리지 않는다 — 결재 판정에 필요한 값(Level1~3 컬럼)이 이미 각 문서
// 엔티티에 있고, 새 컬럼(기안자 등)을 추가하는 건 부담스럽다는 사용자 결정이다.
//
// 결재 대상 판정은 단건 결재 API와 완전히 같은 규칙을 쓴다(ApprovalRules/ApprovalLevels):
//   내 역할에 결재 레벨이 있고 / 그 레벨이 이 문서유형에서 Required이고 /
//   아직 내 단계가 미승인이고 / 이전 단계가 완료됐고 / (교육훈련 외에는) 담당 장비일 것.
// 미운영 장비의 문서도 결재 대상에 포함한다(사용자 결정).
//
// 조회 기간 제한은 두지 않는다 — 결재는 밀린 것일수록 처리해야 하는데, 기간 컷오프가 있으면
// 그 기간을 넘긴 미결재 문서는 목록에서 사라져 영영 결재할 방법이 없어진다(사용자 결정
// 2026-09-28). 대신 "내가 지금 결재 가능한" 조건을 전부 SQL에서 걸러 응답이 커지지 않게 한다.
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController(AppDbContext db) : ControllerBase
{
    //--------------------------------------------------------------------------------//
    // GET api/approvals/pending
    // 내가 지금 결재해야 하는 문서를 문서유형별로 묶어서 반환한다. 파라미터는 없다.
    // 결재할 게 없는 문서유형도 count: 0으로 내려간다(화면 좌측에 흐리게 표시하기 위함).
    // 결재 권한이 없는 역할(시스템관리자/일반관리원)은 전 그룹이 0이다.
    //--------------------------------------------------------------------------------//
    [HttpGet("pending")]
    public async Task<ActionResult<PendingApprovalsDto>> GetPending()
    {
        if (!User.TryGetUser(out int userId, out var role))
            return Unauthorized();

        int? level = ApprovalRules.LevelForRole(role);
        var groups = new List<PendingApprovalGroupDto>();

        foreach (var type in ApprovalDocuments.All)
        {
            var items = level is { } myLevel
                ? await ItemsAsync(type, userId, myLevel)
                : [];

            groups.Add(new PendingApprovalGroupDto(
                type.ToString(), ApprovalDocuments.LabelFor(type), items.Count, items));
        }

        return Ok(new PendingApprovalsDto(groups));
    }

    //--------------------------------------------------------------------------------//
    // GET api/approvals/pending/count — 상단바 배지용. 목록 없이 건수만 센다.
    // 배지는 30~60초마다 갱신되므로 목록(pending)을 그대로 쓰면 낭비다(프론트 요청).
    //--------------------------------------------------------------------------------//
    [HttpGet("pending/count")]
    public async Task<ActionResult<PendingApprovalCountDto>> GetPendingCount()
    {
        if (!User.TryGetUser(out int userId, out var role))
            return Unauthorized();

        int? level = ApprovalRules.LevelForRole(role);
        var byDocType = new Dictionary<string, int>();

        foreach (var type in ApprovalDocuments.All)
            byDocType[type.ToString()] = level is { } myLevel ? await CountAsync(type, userId, myLevel) : 0;

        return Ok(new PendingApprovalCountDto(byDocType.Values.Sum(), byDocType));
    }

    //--------------------------------------------------------------------------------//
    // POST api/approvals/bulk — 한 문서유형의 여러 건을 한 번에 승인한다.
    // 전부-아니면-전무가 아니라 건별로 처리한다. 목록을 받은 뒤 남이 먼저 결재했거나 문서가
    // 지워졌을 수 있는데, 한 건 때문에 전체가 롤백되면 사용자는 어디까지 됐는지 알 수 없다.
    // 개수 상한은 없다(사용자 결정).
    //--------------------------------------------------------------------------------//
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkApprovalResultDto>> ApproveBulk(BulkApprovalRequest request)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest("결재할 문서를 선택하세요.");

        if (!User.TryGetUser(out int userId, out var role))
            return Unauthorized();

        if (ApprovalRules.LevelForRole(role) is not { } level)
            return Forbid();

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized();

        string label = ApprovalDocuments.LabelFor(request.DocType);
        var modes = ApprovalDocuments.ModesFor(request.DocType);
        var succeeded = new List<int>();
        var failed = new List<BulkApprovalFailureDto>();

        // 내 레벨이 이 문서유형의 결재 단계가 아니면(예: 안전관리원 + 수리일지) 전부 실패다.
        if (modes[level] != ApprovalLevelMode.Required)
        {
            foreach (int id in request.Ids.Distinct())
                failed.Add(new(id, "Forbidden", $"{role}은(는) {label}의 결재 대상이 아닙니다."));

            return Ok(new BulkApprovalResultDto(succeeded, failed));
        }

        // 같은 id가 두 번 들어와도 한 번만 처리한다(두 번째가 "이미 결재됨"으로 보이면 더 헷갈린다).
        var ids = request.Ids.Distinct().ToList();
        var documents = await LoadAsync(request.DocType, ids);
        var myEquipmentIds = await MyEquipmentIdsAsync(userId);

        foreach (int id in ids)
        {
            if (!documents.TryGetValue(id, out var doc))
            {
                failed.Add(new(id, "NotFound", "문서를 찾을 수 없습니다."));
                continue;
            }

            if (doc.EquipmentId is { } equipmentId && !myEquipmentIds.Contains(equipmentId))
            {
                failed.Add(new(id, "Forbidden", "담당 장비가 아닙니다."));
                continue;
            }

            // AlreadyApproved와 WrongOrder를 구분해서 돌려준다 — 프론트가 사유별로 분기한다.
            if (ApprovalLevels.Get(doc.Entity, level).ApprovedAt is not null)
            {
                failed.Add(new(id, "AlreadyApproved", "이미 결재된 문서입니다."));
                continue;
            }

            if (!ApprovalLevels.CanApprove(doc.Entity, modes, level))
            {
                failed.Add(new(id, "WrongOrder", "이전 단계 결재가 아직 끝나지 않았습니다."));
                continue;
            }

            ApprovalLevels.Set(doc.Entity, level, userId, user.FullName, DateTimeOffset.UtcNow);
            EventLogger.Add(db, EventLogCategory.Approval,
                $"{user.FullName}({user.Role})님이 {label}({doc.Description})를 일괄 결재했습니다.",
                user.Username, doc.EquipmentId);

            succeeded.Add(id);
        }

        // 승인 + 이벤트 기록을 한 번에 저장한다. 실패 건은 애초에 아무것도 바꾸지 않았으므로
        // "한 건 실패해도 나머지는 그대로 커밋"이 그대로 성립한다.
        await db.SaveChangesAsync();

        return Ok(new BulkApprovalResultDto(succeeded, failed));
    }

    //--------------------------------------------------------------------------------//
    // 문서유형별 조회. "내가 지금 결재 가능한" 조건을 SQL에서 전부 걸러서, 기간 제한 없이도
    // 승인 끝난 문서를 메모리로 끌어오지 않는다.
    //--------------------------------------------------------------------------------//
    private async Task<List<PendingApprovalItemDto>> ItemsAsync(ApprovalDocumentType type, int userId, int level)
    {
        if (ApprovalDocuments.ModesFor(type)[level] != ApprovalLevelMode.Required)
            return [];

        string myLevel = $"level{level}";

        switch (type)
        {
            case ApprovalDocumentType.InspectionLog:
                return await Pending(InspectionQuery(userId), level)
                    .Join(db.Equipments, l => l.EquipmentId, e => e.Id, (l, e) => new { l, e })
                    .OrderByDescending(x => x.l.WeekStartDate).ThenBy(x => x.e.BuildingName).ThenBy(x => x.e.Name)
                    .Select(x => new PendingApprovalItemDto(x.l.Id, x.e.Name,
                        Subtitle(x.e.Region, x.e.BuildingName, x.e.Location),
                        WeekText(x.l.WeekStartDate), null, myLevel))
                    .ToListAsync();

            case ApprovalDocumentType.OperationLog:
                return await Pending(OperationQuery(userId), level)
                    .Join(db.Equipments, l => l.EquipmentId, e => e.Id, (l, e) => new { l, e })
                    .OrderByDescending(x => x.l.Date).ThenBy(x => x.e.BuildingName).ThenBy(x => x.e.Name)
                    .Select(x => new PendingApprovalItemDto(x.l.Id, x.e.Name,
                        Subtitle(x.e.Region, x.e.BuildingName, x.e.Location),
                        DayText(x.l.Date), null, myLevel))
                    .ToListAsync();

            // 교육훈련은 장비에 매달리지 않는 전사 문서라 담당 장비를 보지 않는다.
            case ApprovalDocumentType.TrainingLog:
                return (await Pending(db.TrainingLogs, level)
                        .OrderByDescending(l => l.PerformedAt)
                        .ToListAsync())
                    .Select(l => new PendingApprovalItemDto(l.Id, l.Title, null,
                        DayText(l.PerformedAt), l.CreatedByUserName, myLevel))
                    .ToList();

            case ApprovalDocumentType.RepairLog:
                return (await Pending(RepairQuery(userId), level)
                        .Join(db.Equipments, l => l.EquipmentId, e => e.Id, (l, e) => new { l, e })
                        .OrderByDescending(x => x.l.PerformedAt).ThenBy(x => x.e.BuildingName).ThenBy(x => x.e.Name)
                        .ToListAsync())
                    // 수리일지만 subtitle 끝에 문서 제목을 붙인다 — 한 장비에 같은 날 여러 건이
                    // 쌓일 수 있어서(유니크 제약이 없다) 장비명+일자만으로는 목록에서 구분이
                    // 안 된다. 되돌리기 어려운 동작이라 구분이 필요하다는 프론트 요청(2026-09-28).
                    .Select(x => new PendingApprovalItemDto(x.l.Id, x.e.Name,
                        Subtitle(x.e.Region, x.e.BuildingName, x.e.Location, x.l.Title),
                        DayText(x.l.PerformedAt), x.l.CreatedByUserName, myLevel))
                    .ToList();

            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private async Task<int> CountAsync(ApprovalDocumentType type, int userId, int level)
    {
        if (ApprovalDocuments.ModesFor(type)[level] != ApprovalLevelMode.Required)
            return 0;

        return type switch
        {
            ApprovalDocumentType.InspectionLog => await Pending(InspectionQuery(userId), level).CountAsync(),
            ApprovalDocumentType.OperationLog => await Pending(OperationQuery(userId), level).CountAsync(),
            ApprovalDocumentType.TrainingLog => await Pending(db.TrainingLogs, level).CountAsync(),
            ApprovalDocumentType.RepairLog => await Pending(RepairQuery(userId), level).CountAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private IQueryable<InspectionLog> InspectionQuery(int userId) =>
        db.InspectionLogs.Where(l => db.UserEquipments.Any(ue => ue.UserId == userId && ue.EquipmentId == l.EquipmentId));

    private IQueryable<OperationLog> OperationQuery(int userId) =>
        db.OperationLogs.Where(l => db.UserEquipments.Any(ue => ue.UserId == userId && ue.EquipmentId == l.EquipmentId));

    private IQueryable<RepairLog.Models.RepairLog> RepairQuery(int userId) =>
        db.RepairLogs.Where(l => db.UserEquipments.Any(ue => ue.UserId == userId && ue.EquipmentId == l.EquipmentId));

    //--------------------------------------------------------------------------------//
    // "내 단계가 미승인 + 이전 단계 완료" 필터. ApprovalLevels와 같은 판정이지만, 이건 SQL로
    // 번역되어야 해서 엔티티 타입마다 따로 쓴다(인터페이스 속성은 EF가 번역하지 못한다).
    // 1단계는 앞 단계가 없고, 2단계 앞의 1단계는 점검·운전일지만 Required이며(수리·교육은
    // 해당없음 = 이미 완료로 본다), 3단계 앞의 2단계는 네 문서유형 모두 Required다.
    //--------------------------------------------------------------------------------//
    private static IQueryable<InspectionLog> Pending(IQueryable<InspectionLog> q, int level) => level switch
    {
        1 => q.Where(l => l.Level1ApprovedAt == null),
        2 => q.Where(l => l.Level2ApprovedAt == null && l.Level1ApprovedAt != null),
        _ => q.Where(l => l.Level3ApprovedAt == null && l.Level2ApprovedAt != null)
    };

    private static IQueryable<OperationLog> Pending(IQueryable<OperationLog> q, int level) => level switch
    {
        1 => q.Where(l => l.Level1ApprovedAt == null),
        2 => q.Where(l => l.Level2ApprovedAt == null && l.Level1ApprovedAt != null),
        _ => q.Where(l => l.Level3ApprovedAt == null && l.Level2ApprovedAt != null)
    };

    // 수리일지/교육훈련은 1단계가 "해당없음"이라 2단계가 곧 첫 단계다.
    private static IQueryable<RepairLog.Models.RepairLog> Pending(IQueryable<RepairLog.Models.RepairLog> q, int level) => level switch
    {
        2 => q.Where(l => l.Level2ApprovedAt == null),
        _ => q.Where(l => l.Level3ApprovedAt == null && l.Level2ApprovedAt != null)
    };

    private static IQueryable<TrainingLog.Models.TrainingLog> Pending(IQueryable<TrainingLog.Models.TrainingLog> q, int level) => level switch
    {
        2 => q.Where(l => l.Level2ApprovedAt == null),
        _ => q.Where(l => l.Level3ApprovedAt == null && l.Level2ApprovedAt != null)
    };

    private async Task<HashSet<int>> MyEquipmentIdsAsync(int userId) =>
        (await db.UserEquipments.Where(ue => ue.UserId == userId).Select(ue => ue.EquipmentId).ToListAsync())
            .ToHashSet();

    // 화면 둘째 줄: "A 지구 · PT 배기환경시험동 · 2F". 빈 칸은 빼고 ' · '로 잇는다.
    // extra는 수리일지의 문서 제목처럼 뒤에 덧붙일 구분값이다.
    private static string Subtitle(string region, string buildingName, string? location, string? extra = null) =>
        string.Join(" · ", new[] { region, buildingName, location, extra }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    // 점검일지는 주 단위라 "2026.09.20 ~ 09.26"으로 보여준다(달이 바뀌면 "2026.09.27 ~ 10.03").
    private static string WeekText(DateOnly weekStart) =>
        $"{weekStart:yyyy.MM.dd} ~ {weekStart.AddDays(6):MM.dd}";

    private static string DayText(DateOnly date) => date.ToString("yyyy.MM.dd");

    private static string DayText(DateTimeOffset value) =>
        value.ToOffset(KoreanTime.Offset).ToString("yyyy.MM.dd");

    //--------------------------------------------------------------------------------//
    // 일괄 승인용 조회. EquipmentId는 교육훈련만 null이고, Description은 이벤트 로그
    // 메시지에 쓴다(단건 결재 API가 남기는 문구와 같은 형식).
    //--------------------------------------------------------------------------------//
    private async Task<Dictionary<int, (IApprovable Entity, int? EquipmentId, string Description)>> LoadAsync(
        ApprovalDocumentType type, List<int> ids) => type switch
        {
            ApprovalDocumentType.InspectionLog => (await db.InspectionLogs.Where(l => ids.Contains(l.Id)).ToListAsync())
                .ToDictionary(l => l.Id, l => ((IApprovable)l, (int?)l.EquipmentId, $"장비ID {l.EquipmentId}, {l.WeekStartDate} 주")),
            ApprovalDocumentType.OperationLog => (await db.OperationLogs.Where(l => ids.Contains(l.Id)).ToListAsync())
                .ToDictionary(l => l.Id, l => ((IApprovable)l, (int?)l.EquipmentId, $"장비ID {l.EquipmentId}, {l.Date}")),
            ApprovalDocumentType.TrainingLog => (await db.TrainingLogs.Where(l => ids.Contains(l.Id)).ToListAsync())
                .ToDictionary(l => l.Id, l => ((IApprovable)l, (int?)null, l.Title)),
            ApprovalDocumentType.RepairLog => (await db.RepairLogs.Where(l => ids.Contains(l.Id)).ToListAsync())
                .ToDictionary(l => l.Id, l => ((IApprovable)l, (int?)l.EquipmentId, $"장비ID {l.EquipmentId}, {l.Title}")),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}

//--------------------------------------------------------------------------------//
// 조회 응답. docType은 요청에 그대로 다시 실어 보내는 식별자이고, label은 화면에 쓰는 한글
// 이름이다. 결재할 게 없는 문서유형도 count 0으로 내려간다.
// 교육훈련은 장비가 없어 subtitle이 null이고 title에 문서 제목이 들어간다.
//--------------------------------------------------------------------------------//
public record PendingApprovalsDto(List<PendingApprovalGroupDto> Groups);

public record PendingApprovalGroupDto(string DocType, string Label, int Count, List<PendingApprovalItemDto> Items);

public record PendingApprovalItemDto(int Id, string Title, string? Subtitle, string PeriodText,
    string? AuthorName, string MyLevel);

public record PendingApprovalCountDto(int Total, Dictionary<string, int> ByDocType);

public record BulkApprovalRequest(ApprovalDocumentType DocType, List<int> Ids);

public record BulkApprovalResultDto(List<int> Succeeded, List<BulkApprovalFailureDto> Failed);

public record BulkApprovalFailureDto(int Id, string Reason, string Message);
