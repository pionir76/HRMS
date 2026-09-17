namespace HRMS.Modules.Notice.Models;

//--------------------------------------------------------------------------------//
// 공지사항 1건. 결재가 없는 가장 단순한 문서다(교육훈련 일지에서 결재만 뺀 형태).
// 장비와도 무관하다.
//
// IsPinned(상단 고정): 목록에서 항상 최상단에 노출된다. **시스템관리자만** 설정/해제할 수 있어서
// 일반 수정(PUT)이 아니라 별도 엔드포인트(POST {id}/pin, {id}/unpin)로 분리했다 — 작성자가
// 본문을 수정할 때 실수로/의도적으로 고정 상태를 바꾸는 일이 없도록 하기 위함이다.
//
// 첨부파일은 이 엔티티가 직접 갖지 않고 Modules/Attachment(ownerType=Notice)로 관리한다.
//--------------------------------------------------------------------------------//
public class Notice
{
    public int Id { get; set; }

    public required string Title { get; set; } // 제목
    public string? Content { get; set; } // 내용

    public bool IsPinned { get; set; } // 상단 고정 — 시스템관리자만 변경 가능

    public int CreatedByUserId { get; set; }
    public required string CreatedByUserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
