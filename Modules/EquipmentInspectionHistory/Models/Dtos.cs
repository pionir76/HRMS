namespace HRMS.Modules.EquipmentInspectionHistory.Models;

//-----------------------------------------------------------------------------//
// AttachmentCount: 이 레코드에 달린 첨부파일 개수만 가볍게 같이 내려준다(파일 내용은 없음) —
// 목록 화면에서 클립 아이콘을 표시할 때 항목마다 별도 호출 없이 바로 알 수 있게 하기 위함
// (장비 이력카드의 hasEquipmentPhoto/hasInstallationPhoto와 같은 이유).
//
// CanEdit: 지금 요청한 사용자가 이 이력을 수정할 수 있는지를 서버가 계산해서 내려준다
// (시스템관리자거나 작성자 본인이면 true). 응답에는 작성자 "이름"만 있고 로그인 응답에는
// 사용자 Id가 없어서, 이 값이 없으면 프론트가 JWT를 직접 디코드해야 수정 버튼 표시 여부를
// 판단할 수 있다 — 그걸 피하려고 넣은 값이다. 요청 본문에는 당연히 없는 응답 전용 필드.
//-----------------------------------------------------------------------------//
public record EquipmentInspectionHistoryDto(
    int Id,
    int EquipmentId,
    DateOnly Date,
    string Title,
    string Content,
    string? Notes,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    string? UpdatedByUserName,
    DateTimeOffset? UpdatedAt,
    int AttachmentCount,
    bool CanEdit);

public record CreateEquipmentInspectionHistoryRequest(
    int EquipmentId,
    DateOnly Date,
    string Title,
    string Content,
    string? Notes);

// EquipmentId는 생성 후 변경 불가라 여기 포함하지 않는다.
public record UpdateEquipmentInspectionHistoryRequest(
    DateOnly Date,
    string Title,
    string Content,
    string? Notes);
