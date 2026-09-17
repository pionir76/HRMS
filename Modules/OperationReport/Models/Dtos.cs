using HRMS.Modules.Approval;

namespace HRMS.Modules.OperationReport.Models;

public record OperationItemDto(
    string ItemKey, int? CompressorId,
    string? Time0900, string? Time1300, string? Time1600, string? Time2100);

public record OperationReferenceDto(string ItemKey, string? ReferenceText);

//--------------------------------------------------------------------------------//
// 조회 응답. Id가 null이면 그 장비/날짜에 아직 로그가 없다는 뜻이다(자동 기록 서비스가 정상
// 동작 중이면 오늘/과거 날짜에는 거의 항상 생성되어 있어야 한다 — README "공통 규칙" 참고).
//--------------------------------------------------------------------------------//
public record OperationLogDto(
    int? Id,
    int EquipmentId,
    DateOnly Date,
    ApprovalStepDto Level1,
    ApprovalStepDto Level2,
    ApprovalStepDto Level3,
    List<OperationItemDto> Items,
    List<OperationReferenceDto> References);

public record OperationItemInput(
    string ItemKey, int? CompressorId,
    string? Time0900, string? Time1300, string? Time1600, string? Time2100);

public record OperationReferenceInput(string ItemKey, string? ReferenceText);

//--------------------------------------------------------------------------------//
// PUT 요청 본문. 저장할 때마다 Items/References를 통째로 교체한다(점검일지와 같은 방식).
// 결재 상태와 무관하게 항상 저장을 허용한다 — 결재 완료 후 수정 잠금은 프론트가 UI로만 처리한다.
//--------------------------------------------------------------------------------//
public record SaveOperationLogRequest(
    int EquipmentId,
    DateOnly Date,
    List<OperationItemInput> Items,
    List<OperationReferenceInput> References);
