using HRMS.Modules.Approval;

namespace HRMS.Modules.InspectionReport.Models;

public record InspectionResultDto(
    string ItemNo, string Category, string Content,
    string? Sun, string? Mon, string? Tue, string? Wed, string? Thu, string? Fri, string? Sat,
    string? Memo);

//--------------------------------------------------------------------------------//
// 조회 응답. Id가 null이면 그 장비/주차에 아직 저장된 점검일지가 없다는 뜻이다(빈 폼).
// Results는 항상 InspectionItemCatalog의 10개 항목 전부를 포함한다(값이 없으면 필드가 null).
//--------------------------------------------------------------------------------//
public record InspectionLogDto(
    int? Id,
    int EquipmentId,
    DateOnly WeekStartDate,
    string? Opinion,
    ApprovalStepDto Level1,
    ApprovalStepDto Level2,
    ApprovalStepDto Level3,
    List<InspectionResultDto> Results);

public record InspectionResultInput(
    string ItemNo,
    string? Sun, string? Mon, string? Tue, string? Wed, string? Thu, string? Fri, string? Sat,
    string? Memo);

//--------------------------------------------------------------------------------//
// PUT 요청 본문. 저장할 때마다 Results 전체를 통째로 교체한다(부분 diff 없음 — 폼 전체를
// 한 번에 저장하는 방식이라 이게 더 단순하다).
//--------------------------------------------------------------------------------//
public record SaveInspectionLogRequest(
    int EquipmentId,
    DateOnly WeekStartDate,
    string? Opinion,
    List<InspectionResultInput> Results);
