using HRMS.Modules.Approval;

namespace HRMS.Modules.TrainingLog.Models;

//--------------------------------------------------------------------------------//
// 조회/저장 응답. Level1~3은 다른 결재 문서와 동일한 ApprovalStepDto 형태다
// (Level1은 이 문서유형에서 항상 "NotApplicable" — 프론트는 `/`로 표시).
//
// AttachmentCount: 이 문서에 달린 첨부파일 개수(파일 내용은 없음). 목록에서 클립 아이콘을
// 표시할 때 항목마다 별도 호출을 하지 않도록 넣은 값이다(검사이력과 동일).
// CanEdit/CanDelete: 요청자 기준으로 서버가 계산한 수정/삭제 가능 여부(응답 전용).
//--------------------------------------------------------------------------------//
public record TrainingLogDto(
    int Id,
    string Title,
    DateTimeOffset PerformedAt,
    string? Location,
    string? Instructor,
    string? Content,
    string? Attendees,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    string? UpdatedByUserName,
    DateTimeOffset? UpdatedAt,
    ApprovalStepDto Level1,
    ApprovalStepDto Level2,
    ApprovalStepDto Level3,
    int AttachmentCount,
    bool CanEdit,
    bool CanDelete);

//--------------------------------------------------------------------------------//
// POST/PUT 요청 본문. 등록과 수정이 필드 구성이 같아서(장비처럼 생성 후 고정되는 값이 없음)
// 하나의 record를 공유한다.
//--------------------------------------------------------------------------------//
public record SaveTrainingLogRequest(
    string Title,
    DateTimeOffset PerformedAt,
    string? Location,
    string? Instructor,
    string? Content,
    string? Attendees);
