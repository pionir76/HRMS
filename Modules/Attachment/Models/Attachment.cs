namespace HRMS.Modules.Attachment.Models;

//-----------------------------------------------------------------------------//
// 여러 도메인(장비 사진, 장비 검사이력, 교육훈련 일지, 공지사항)에서 공용으로 쓰는 첨부파일.
// OwnerType+OwnerId로 소속 리소스를 가리킨다 — 도메인마다 테이블을 따로 만들지 않는다.
//
// Slot은 OwnerType=EquipmentPhoto처럼 "장비 사진 1장 + 설치 사진 1장"같이 정해진 슬롯이
// 있는 경우에만 쓴다(같은 OwnerType+OwnerId+Slot 조합은 재업로드 시 기존 행을 덮어쓴다 —
// AppDbContext에 이를 강제하는 유니크 인덱스가 있다). 슬롯 개념이 없는 다건 첨부(공지사항 등)는
// Slot이 null이고, 업로드할 때마다 새 행이 그냥 추가된다.
//
// 실제 파일 바이트는 DB가 아니라 파일시스템에 저장한다(AttachmentStorage 참고) — StoredPath는
// 그 파일의 저장소 루트 기준 상대경로다.
//-----------------------------------------------------------------------------//
public class Attachment
{
    public int Id { get; set; }
    public AttachmentOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }
    public string? Slot { get; set; }

    public required string FileName { get; set; } // 사용자가 업로드한 원본 파일명(다운로드 시 그대로 내려줌)
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StoredPath { get; set; } // 저장소 루트 기준 상대경로(GUID 파일명이라 원본과 다름)

    public int UploadedByUserId { get; set; }
    public required string UploadedByUserName { get; set; } // 업로드 시점 이름을 그대로 남긴다(점검일지 ApproverName과 동일한 관례)
    public DateTimeOffset UploadedAt { get; set; }
}
