namespace HRMS.Modules.Approval;

//--------------------------------------------------------------------------------//
// 결재가 붙는 문서(점검일지/운전일지/수리일지/교육훈련 일지)가 공통으로 갖는 승인 필드.
// 승인 데이터는 문서유형별 범용 Approval 테이블이 아니라 각 문서 엔티티의 인라인 컬럼으로
// 저장한다(이유는 api-manual.md "결재 시스템" 절). 이 인터페이스는 그 컬럼들을 문서 종류와
// 무관하게 다루기 위한 것이고, 컬럼 이름이 이미 4개 엔티티에서 동일해서 선언만 붙이면 된다
// — DB 스키마는 전혀 바뀌지 않는다.
//
// 일괄 결재(Modules/Approval/Controllers/ApprovalsController)가 문서 4종을 가로질러
// 같은 판정을 해야 해서 2026-09-28에 도입했다.
//--------------------------------------------------------------------------------//
public interface IApprovable
{
    int? Level1ApproverId { get; set; }
    string? Level1ApproverName { get; set; }
    DateTimeOffset? Level1ApprovedAt { get; set; }

    int? Level2ApproverId { get; set; }
    string? Level2ApproverName { get; set; }
    DateTimeOffset? Level2ApprovedAt { get; set; }

    int? Level3ApproverId { get; set; }
    string? Level3ApproverName { get; set; }
    DateTimeOffset? Level3ApprovedAt { get; set; }
}
