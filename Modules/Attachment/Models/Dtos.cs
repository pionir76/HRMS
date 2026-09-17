namespace HRMS.Modules.Attachment.Models;

//-----------------------------------------------------------------------------//
// 첨부파일 메타데이터 응답. 실제 바이너리는 GET /api/attachments/{id}로 별도 조회한다
// (목록/조회 API에는 파일 내용을 절대 싣지 않는다 — Doc/api-manual.md 참고).
//-----------------------------------------------------------------------------//
public record AttachmentDto(
    int Id,
    string OwnerType,
    int OwnerId,
    string? Slot,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedByUserName,
    DateTimeOffset UploadedAt);
