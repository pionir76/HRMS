namespace HRMS.Modules.Notice.Models;

//--------------------------------------------------------------------------------//
// 조회/저장 응답.
// AttachmentCount: 첨부파일 개수만 가볍게 알려주는 값(파일 내용은 없음).
// CanEdit/CanDelete: 요청자가 이 공지를 수정/삭제할 수 있는지(= 작성자 또는 시스템관리자).
//   응답에는 작성자 Id가 아니라 이름만 있어서 프론트가 자체 판단할 근거가 없기 때문에 넣는다.
//   상단 고정 가능 여부는 별도 필드를 두지 않았다 — 시스템관리자 여부만 보면 되고, 그건
//   로그인 응답의 role로 프론트가 이미 알 수 있다.
//--------------------------------------------------------------------------------//
public record NoticeDto(
    int Id,
    string Title,
    string? Content,
    bool IsPinned,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    string? UpdatedByUserName,
    DateTimeOffset? UpdatedAt,
    int AttachmentCount,
    bool CanEdit,
    bool CanDelete);

// POST/PUT 요청 본문. IsPinned는 여기 없다 — 관리자 전용 별도 엔드포인트로만 바꾼다.
public record SaveNoticeRequest(
    string Title,
    string? Content);
