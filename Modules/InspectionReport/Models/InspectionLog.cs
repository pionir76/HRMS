namespace HRMS.Modules.InspectionReport.Models;

//--------------------------------------------------------------------------------//
// 점검일지 1건 = 장비 1개 + 주(일~토) 1개. (EquipmentId, WeekStartDate) 조합이 유니크하다.
// 결재 데이터(Level1~3)는 별도 테이블 없이 이 엔티티에 인라인 컬럼으로 직접 둔다
// (Modules/Approval/README.md 참고). ApproverName은 승인 시점 이름 스냅샷이라, 나중에
// 그 사용자의 이름이 바뀌거나 역할이 바뀌어도 이미 찍힌 결재 기록은 그대로 남는다.
//--------------------------------------------------------------------------------//
public class InspectionLog
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateOnly WeekStartDate { get; set; } // 그 주의 일요일 날짜
    public string? Opinion { get; set; } // 점검자 의견

    public int? Level1ApproverId { get; set; } // 안전관리원
    public string? Level1ApproverName { get; set; }
    public DateTimeOffset? Level1ApprovedAt { get; set; }

    public int? Level2ApproverId { get; set; } // 안전관리책임자
    public string? Level2ApproverName { get; set; }
    public DateTimeOffset? Level2ApprovedAt { get; set; }

    public int? Level3ApproverId { get; set; } // 안전관리총괄자
    public string? Level3ApproverName { get; set; }
    public DateTimeOffset? Level3ApprovedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<InspectionResult> Results { get; set; } = [];
}
