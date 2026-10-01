using HRMS.Modules.Approval;

namespace HRMS.Modules.TrainingLog.Models;

//--------------------------------------------------------------------------------//
// 교육훈련 일지 1건. 다른 결재 문서들과 달리 **장비에 매달리지 않는 전사 문서**라
// EquipmentId가 없다 — 그래서 결재 승인 시 담당 장비(UserEquipment)를 검사하지 않고
// 역할만 본다(사용자 결정, Modules/TrainingLog/README.md 참고).
//
// 결재 데이터(Level1~3)는 별도 테이블 없이 여기에 인라인 컬럼으로 직접 둔다(점검일지/운전일지/
// 수리일지와 동일 — Modules/Approval/README.md 참고). Level1(안전관리원)은 이 문서유형에서
// "해당없음"이라 실제로 채워지지 않지만, 공용 결재 로직이 1~3을 그대로 훑으므로 컬럼은 남겨둔다.
//
// 첨부파일은 이 엔티티가 직접 갖지 않고 Modules/Attachment(ownerType=TrainingLog,
// ownerId=이 레코드의 Id)로 관리한다. 참석자 명단은 시스템 계정과 무관한 단순 문자열이다.
//--------------------------------------------------------------------------------//
public class TrainingLog : IApprovable
{
    public int Id { get; set; }

    public required string Title { get; set; } // 제목
    public DateTimeOffset PerformedAt { get; set; } // 일시(날짜+시각)
    public string? Location { get; set; } // 장소
    public string? Instructor { get; set; } // 강사
    public string? Content { get; set; } // 교육내용
    public string? Attendees { get; set; } // 참석자 명단 — 단순 문자열(Users 테이블과 무관)

    public int CreatedByUserId { get; set; }
    public required string CreatedByUserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public int? Level1ApproverId { get; set; } // 안전관리원 — 이 문서유형에서는 "해당없음"이라 항상 비어있다
    public string? Level1ApproverName { get; set; }
    public DateTimeOffset? Level1ApprovedAt { get; set; }

    public int? Level2ApproverId { get; set; } // 안전관리책임자
    public string? Level2ApproverName { get; set; }
    public DateTimeOffset? Level2ApprovedAt { get; set; }

    public int? Level3ApproverId { get; set; } // 안전관리총괄자
    public string? Level3ApproverName { get; set; }
    public DateTimeOffset? Level3ApprovedAt { get; set; }
}
