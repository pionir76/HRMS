using HRMS.Modules.Auth.Models;

namespace HRMS.Modules.Approval;

//--------------------------------------------------------------------------------//
// 결재 판정 공용 로직. 점검일지/운전일지/교육훈련/수리보수일지 등 결재가 필요한 문서마다
// 승인 데이터(Level1~3의 ApproverId/ApproverName/ApprovedAt)는 각 문서 엔티티에 인라인
// 컬럼으로 직접 갖고 있지만(문서유형별로 흩어진 범용 Approval 테이블을 안 쓰는 이유는
// api-manual.md 결재 시스템 절 참고), 순서/권한 판정 규칙은 여기 하나로 공유한다.
//--------------------------------------------------------------------------------//
public static class ApprovalRules
{
    // 로그인 사용자의 Role이 결재 몇 번 레벨(1=안전관리원, 2=안전관리책임자, 3=안전관리총괄자)에
    // 해당하는지. 그 외 역할(시스템관리자/일반관리원)은 결재 대상이 아니라 null.
    public static int? LevelForRole(UserRole role) => role switch
    {
        UserRole.안전관리원 => 1,
        UserRole.안전관리책임자 => 2,
        UserRole.안전관리총괄자 => 3,
        _ => null
    };

    // 이 레벨이 "완료된 것으로" 취급되는지 — 실제 승인됐거나, 애초에 결재 절차가 없는
    // 레벨(NotApplicable/Delegated)이면 완료로 본다. 상위 레벨의 순서 판정에 쓰인다.
    public static bool IsSatisfied(ApprovalLevelMode mode, DateTimeOffset? approvedAt) =>
        mode != ApprovalLevelMode.Required || approvedAt is not null;

    // 이 레벨을 지금 승인할 수 있는지 — Required 레벨이어야 하고, 아직 미승인이어야 하고,
    // 하위(이전) 레벨이 먼저 완료돼 있어야 한다(순서 강제).
    public static bool CanApprove(ApprovalLevelMode mode, DateTimeOffset? approvedAt, bool previousLevelSatisfied) =>
        mode == ApprovalLevelMode.Required && approvedAt is null && previousLevelSatisfied;

    // 이 레벨의 승인을 취소할 수 있는지 — 이미 승인된 상태여야 하고, 상위(다음) 레벨이
    // 아직 승인되지 않았어야 한다("상위 결재 전이면 취소 가능").
    public static bool CanCancel(DateTimeOffset? approvedAt, bool nextLevelSatisfied) =>
        approvedAt is not null && !nextLevelSatisfied;

    // 조회 응답용 상태 문자열로 변환.
    public static ApprovalStepDto ToDto(ApprovalLevelMode mode, string? approverName, DateTimeOffset? approvedAt) =>
        mode switch
        {
            ApprovalLevelMode.NotApplicable => new ApprovalStepDto("NotApplicable", null, null),
            ApprovalLevelMode.Delegated => new ApprovalStepDto("Delegated", null, null),
            _ => new ApprovalStepDto(approvedAt is null ? "Pending" : "Approved", approverName, approvedAt)
        };
}
