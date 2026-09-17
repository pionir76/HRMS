using HRMS.Modules.Approval;

namespace HRMS.Modules.RepairLog.Models;

//--------------------------------------------------------------------------------//
// 조회/저장 응답. Level1~3은 점검일지와 동일한 ApprovalStepDto 형태다
// (Level1은 이 문서유형에서 항상 "NotApplicable" — 프론트는 `/`로 표시한다).
//
// CanEdit/CanDelete: 지금 요청한 사용자가 이 문서를 수정/삭제할 수 있는지를 서버가 계산해서
// 내려준다. 조건이 "결재 진행 여부 + 작성자/관리자" 조합이라 프론트가 스스로 판단하기 어렵고,
// 응답에는 작성자 Id가 아니라 이름만 있어서 비교 근거도 없다(검사이력의 CanEdit과 같은 이유).
// 응답 전용 필드라 요청 본문에는 없다.
//--------------------------------------------------------------------------------//
public record RepairLogDto(
    int Id,
    int EquipmentId,
    string Title,
    DateTimeOffset PerformedAt,
    string? Location,
    string? PerformedBy,
    string? Target,
    string? StateBefore,
    string? StateAfter,
    string? Result,
    string? FailureCause,
    string? PreventiveAction,
    string? Opinion,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    string? UpdatedByUserName,
    DateTimeOffset? UpdatedAt,
    ApprovalStepDto Level1,
    ApprovalStepDto Level2,
    ApprovalStepDto Level3,
    bool CanEdit,
    bool CanDelete);

//--------------------------------------------------------------------------------//
// POST 요청 본문(신규 등록).
//--------------------------------------------------------------------------------//
public record CreateRepairLogRequest(
    int EquipmentId,
    string Title,
    DateTimeOffset PerformedAt,
    string? Location,
    string? PerformedBy,
    string? Target,
    string? StateBefore,
    string? StateAfter,
    string? Result,
    string? FailureCause,
    string? PreventiveAction,
    string? Opinion);

// PUT 요청 본문. EquipmentId는 생성 후 변경 불가라 여기 포함하지 않는다.
public record UpdateRepairLogRequest(
    string Title,
    DateTimeOffset PerformedAt,
    string? Location,
    string? PerformedBy,
    string? Target,
    string? StateBefore,
    string? StateAfter,
    string? Result,
    string? FailureCause,
    string? PreventiveAction,
    string? Opinion);
