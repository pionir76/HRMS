using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Attachment;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Notice.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Notice.Controllers;

//--------------------------------------------------------------------------------//
// 공지사항 API. 결재가 없는 가장 단순한 문서다(교육훈련 일지에서 결재만 뺀 형태).
//
// 권한 규칙(사용자 결정):
//   - 조회/등록: 로그인한 사용자 누구나
//   - 수정/삭제: 작성자 또는 시스템관리자
//   - 상단 고정/해제: 시스템관리자만 (별도 엔드포인트)
//   - 첨부파일 추가/삭제: 수정과 동일한 조건(AttachmentsController가 판정)
//
// 목록은 상단 고정된 공지가 항상 먼저 나오고, 그 다음 작성일시 내림차순이다.
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/notices")]
[Authorize]
public class NoticesController(AppDbContext db, AttachmentStorage storage) : ControllerBase
{
    // GET api/notices — 전체 목록. 고정 공지 우선, 그 다음 최신순. 필터/페이징 없음(사용자 결정).
    [HttpGet]
    public async Task<ActionResult<List<NoticeDto>>> GetList()
    {
        var notices = await db.Notices
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();

        var counts = await GetAttachmentCountsAsync(notices.Select(n => n.Id));
        return Ok(notices.Select(n => ToDto(n, counts.GetValueOrDefault(n.Id))).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NoticeDto>> GetOne(int id)
    {
        var notice = await db.Notices.FindAsync(id);
        if (notice is null)
            return NotFound();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(notice, counts.GetValueOrDefault(id)));
    }

    [HttpPost]
    public async Task<ActionResult<NoticeDto>> Create(SaveNoticeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");

        var (userId, userName) = await CurrentUserAsync();

        var notice = new Models.Notice
        {
            Title = request.Title,
            Content = request.Content,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notices.Add(notice);
        await db.SaveChangesAsync();
        return Ok(ToDto(notice, 0));
    }

    // PUT api/notices/{id} — 작성자 또는 시스템관리자만. 상단 고정 상태는 이 API로 바뀌지 않는다.
    [HttpPut("{id}")]
    public async Task<ActionResult<NoticeDto>> Update(int id, SaveNoticeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("제목은 비어있을 수 없습니다.");

        var notice = await db.Notices.FindAsync(id);
        if (notice is null)
            return NotFound();

        var (userId, userName) = await CurrentUserAsync();
        if (!IsSystemAdmin && notice.CreatedByUserId != userId)
            return Forbid();

        notice.Title = request.Title;
        notice.Content = request.Content;
        notice.UpdatedByUserId = userId;
        notice.UpdatedByUserName = userName;
        notice.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(notice, counts.GetValueOrDefault(id)));
    }

    // DELETE api/notices/{id} — 작성자 또는 시스템관리자만. 딸린 첨부파일도 같이 정리한다.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var notice = await db.Notices.FindAsync(id);
        if (notice is null)
            return NotFound();

        if (!IsSystemAdmin && notice.CreatedByUserId != CurrentUserId)
            return Forbid();

        var attachments = await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.Notice && a.OwnerId == id)
            .ToListAsync();
        foreach (var a in attachments)
            storage.Delete(a.StoredPath);
        db.Attachments.RemoveRange(attachments);

        db.Notices.Remove(notice);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/notices/{id}/pin — 상단 고정. 시스템관리자 전용.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/pin")]
    public async Task<ActionResult<NoticeDto>> Pin(int id) => await SetPinnedAsync(id, true);

    // POST api/notices/{id}/unpin — 상단 고정 해제. 시스템관리자 전용.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id}/unpin")]
    public async Task<ActionResult<NoticeDto>> Unpin(int id) => await SetPinnedAsync(id, false);

    private async Task<ActionResult<NoticeDto>> SetPinnedAsync(int id, bool pinned)
    {
        var notice = await db.Notices.FindAsync(id);
        if (notice is null)
            return NotFound();

        notice.IsPinned = pinned;
        await db.SaveChangesAsync();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(notice, counts.GetValueOrDefault(id)));
    }

    private bool IsSystemAdmin => User.IsSystemAdmin();

    private int CurrentUserId => User.GetUserId();

    private bool CanManage(Models.Notice notice) => IsSystemAdmin || notice.CreatedByUserId == CurrentUserId;

    private async Task<(int UserId, string UserName)> CurrentUserAsync()
    {
        var userId = CurrentUserId;
        var userName = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync();
        return (userId, userName);
    }

    private async Task<Dictionary<int, int>> GetAttachmentCountsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        return await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.Notice && idList.Contains(a.OwnerId))
            .GroupBy(a => a.OwnerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    // CanEdit/CanDelete가 요청자 기준 계산값이라 static이 아니다. 수정과 삭제 조건이 같아서
    // 두 값은 항상 동일하지만, 프론트가 버튼별로 쓰기 편하도록 따로 내려준다.
    private NoticeDto ToDto(Models.Notice n, int attachmentCount) => new(
        n.Id, n.Title, n.Content, n.IsPinned,
        n.CreatedByUserName, n.CreatedAt, n.UpdatedByUserName, n.UpdatedAt,
        attachmentCount, CanManage(n), CanManage(n));
}
