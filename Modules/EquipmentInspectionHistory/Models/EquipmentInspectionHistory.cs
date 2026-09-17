namespace HRMS.Modules.EquipmentInspectionHistory.Models;

//-----------------------------------------------------------------------------//
// 장비별 검사/점검 이력 한 건. 기존 InspectionReport(주간 점검일지, 고정 10항목 체크리스트)와는
// 완전히 별개다 — 이쪽은 장비 이력카드에서 자유 서술형으로 등록/조회/편집하는 이력 기록이다.
// 결재 없음, 담당 장비 제약 없음(로그인한 누구나 등록/편집/삭제 가능 — 사용자 결정).
// 첨부파일은 이 엔티티가 직접 갖지 않고 Modules/Attachment(ownerType=EquipmentInspectionHistory,
// ownerId=이 레코드의 Id)로 따로 관리한다.
//-----------------------------------------------------------------------------//
public class EquipmentInspectionHistory
{
    public int Id { get; set; }
    public int EquipmentId { get; set; } // 생성 후 변경 불가(PUT 요청에 포함하지 않음)
    public DateOnly Date { get; set; } // 실시 일자
    public required string Title { get; set; }
    public required string Content { get; set; } // 검사내용
    public string? Notes { get; set; } // 비고

    public int CreatedByUserId { get; set; }
    public required string CreatedByUserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
