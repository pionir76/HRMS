namespace HRMS.Modules.OperationReport.Models;

//--------------------------------------------------------------------------------//
// 운전일지 1건 = 장비 1개 + 날짜 1개(일 단위). 결재 데이터(Level1~3)는 점검일지와 같은 방식으로
// 인라인 컬럼에 직접 둔다(Modules/Approval/README.md 참고). 점검일지와 달리 결재 진행 여부와
// 무관하게 수정이 항상 허용된다 — 결재 완료 후 수정 잠금은 프론트가 UI로만 처리한다(README 참고).
//--------------------------------------------------------------------------------//
public class OperationLog
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateOnly Date { get; set; }

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

    public List<OperationItemValue> Items { get; set; } = [];
    public List<OperationReferenceValue> References { get; set; } = [];
}
