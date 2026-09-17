namespace HRMS.Modules.InspectionReport.Models;

//--------------------------------------------------------------------------------//
// 점검일지(InspectionLog) 안의 점검항목 1개(ItemNo)에 대한 요일별 결과 + 항목별 비고.
// ItemNo는 InspectionItemCatalog에 정의된 고정 항목번호("1".."6-3")와 매칭된다.
// 요일별 값은 "O"(정상) / "/"(미해당) / "X"(이상) 중 하나이거나, 아직 미기록이면 null이다.
//--------------------------------------------------------------------------------//
public class InspectionResult
{
    public int LogId { get; set; }
    public string ItemNo { get; set; } = "";

    public string? Sun { get; set; }
    public string? Mon { get; set; }
    public string? Tue { get; set; }
    public string? Wed { get; set; }
    public string? Thu { get; set; }
    public string? Fri { get; set; }
    public string? Sat { get; set; }

    public string? Memo { get; set; }
}
