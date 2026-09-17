namespace HRMS.Modules.Approval;

//--------------------------------------------------------------------------------//
// 결재 한 단계(레벨)의 조회 응답. Status는 Pending/Approved/NotApplicable/Delegated 중 하나.
// NotApplicable/Delegated일 때는 ApproverName/ApprovedAt이 항상 null이다(애초에 아무도 승인 안 함).
//--------------------------------------------------------------------------------//
public record ApprovalStepDto(string Status, string? ApproverName, DateTimeOffset? ApprovedAt);
