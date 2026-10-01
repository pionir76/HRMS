namespace HRMS.Modules.Approval;

//--------------------------------------------------------------------------------//
// 문서유형 하나의 결재 3단계 운영 방식 묶음. 값은 ApprovalDocuments.ModesFor에서 온다.
//--------------------------------------------------------------------------------//
public readonly record struct ApprovalLevelModes(
    ApprovalLevelMode Level1, ApprovalLevelMode Level2, ApprovalLevelMode Level3)
{
    public ApprovalLevelMode this[int level] => level switch
    {
        1 => Level1,
        2 => Level2,
        3 => Level3,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}

//--------------------------------------------------------------------------------//
// 결재 레벨 읽기/쓰기 공용 코드. 점검일지·운전일지·수리일지·교육훈련 컨트롤러가 각자
// 똑같은 GetLevel/SetLevel/IsLevelSatisfied를 private으로 갖고 있었는데(엔티티 타입만
// 다르고 본문은 동일), 일괄 결재가 다섯 번째 복사본이 되지 않도록 여기로 모았다.
//
// 판정 규칙 자체(순서 강제, 취소 가능 조건)는 그대로 ApprovalRules에 있고, 이 클래스는
// "IApprovable의 Level1~3 컬럼을 레벨 번호로 다룬다"는 부분만 담당한다.
//--------------------------------------------------------------------------------//
public static class ApprovalLevels
{
    public const int Count = 3;

    public static (int? ApproverId, string? ApproverName, DateTimeOffset? ApprovedAt) Get(IApprovable doc, int level) => level switch
    {
        1 => (doc.Level1ApproverId, doc.Level1ApproverName, doc.Level1ApprovedAt),
        2 => (doc.Level2ApproverId, doc.Level2ApproverName, doc.Level2ApprovedAt),
        3 => (doc.Level3ApproverId, doc.Level3ApproverName, doc.Level3ApprovedAt),
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    public static void Set(IApprovable doc, int level, int? approverId, string? approverName, DateTimeOffset? approvedAt)
    {
        switch (level)
        {
            case 1: doc.Level1ApproverId = approverId; doc.Level1ApproverName = approverName; doc.Level1ApprovedAt = approvedAt; break;
            case 2: doc.Level2ApproverId = approverId; doc.Level2ApproverName = approverName; doc.Level2ApprovedAt = approvedAt; break;
            case 3: doc.Level3ApproverId = approverId; doc.Level3ApproverName = approverName; doc.Level3ApprovedAt = approvedAt; break;
            default: throw new ArgumentOutOfRangeException(nameof(level));
        }
    }

    // 결재를 전부 무효화(시스템관리자의 결재 초기화).
    public static void Reset(IApprovable doc)
    {
        for (int level = 1; level <= Count; level++)
            Set(doc, level, null, null, null);
    }

    public static bool IsSatisfied(IApprovable doc, ApprovalLevelModes modes, int level) =>
        ApprovalRules.IsSatisfied(modes[level], Get(doc, level).ApprovedAt);

    // 이 레벨을 지금 승인할 수 있는지 — 이전 단계 완료 여부까지 함께 본다.
    public static bool CanApprove(IApprovable doc, ApprovalLevelModes modes, int level)
    {
        bool previousSatisfied = level == 1 || IsSatisfied(doc, modes, level - 1);
        return ApprovalRules.CanApprove(modes[level], Get(doc, level).ApprovedAt, previousSatisfied);
    }

    // 이 레벨의 승인을 취소할 수 있는지 — 상위 단계가 아직 승인 전이어야 한다.
    // "본인이 승인한 건인지"는 호출부에서 따로 본다(Forbid와 Conflict를 구분해야 해서).
    public static bool CanCancel(IApprovable doc, ApprovalLevelModes modes, int level)
    {
        bool nextSatisfied = level < Count && IsSatisfied(doc, modes, level + 1);
        return ApprovalRules.CanCancel(Get(doc, level).ApprovedAt, nextSatisfied);
    }
}
