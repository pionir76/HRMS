using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Attachment;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.EquipmentInspectionHistory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.EquipmentInspectionHistory.Controllers;

//-----------------------------------------------------------------------------//
// 장비 검사이력 CRUD API. 기존 InspectionReport(점검일지)와 무관 — 결재 없음, 담당 장비
// 제약도 없음. 권한 규칙은 동작별로 다르다(전부 사용자 결정):
//   - 조회/등록: 로그인한 사용자 누구나
//   - 수정: 시스템관리자 또는 작성자 본인만 (2026-09-16 변경, 그 전엔 누구나 가능했음)
//   - 삭제: 시스템관리자만 (2026-09-14)
// 첨부파일은 Modules/Attachment(ownerType=EquipmentInspectionHistory)로 별도 관리하므로,
// 삭제 시 여기서 같이 정리한다.
//-----------------------------------------------------------------------------//
[ApiController]
[Route("api/equipment-inspection-history")]
[Authorize]
public class EquipmentInspectionHistoryController(AppDbContext db, AttachmentStorage storage) : ControllerBase
{
    // GET api/equipment-inspection-history?equipmentId= — 해당 장비의 이력 목록, 최신 날짜순
    [HttpGet]
    public async Task<ActionResult<List<EquipmentInspectionHistoryDto>>> GetList([FromQuery] int equipmentId)
    {
        var records = await db.EquipmentInspectionHistories
            .Where(h => h.EquipmentId == equipmentId)
            .OrderByDescending(h => h.Date)
            .ToListAsync();

        var counts = await GetAttachmentCountsAsync(records.Select(r => r.Id));
        return Ok(records.Select(r => ToDto(r, counts.GetValueOrDefault(r.Id))).ToList());
    }

    // GET api/equipment-inspection-history/{id} — 단건 조회(편집 폼 채울 때)
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentInspectionHistoryDto>> GetOne(int id)
    {
        var record = await db.EquipmentInspectionHistories.FindAsync(id);
        if (record is null)
            return NotFound();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(record, counts.GetValueOrDefault(id)));
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentInspectionHistoryDto>> Create(CreateEquipmentInspectionHistoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("제목과 검사내용은 비어있을 수 없습니다.");
        if (await db.Equipments.FindAsync(request.EquipmentId) is null)
            return NotFound("존재하지 않는 장비입니다.");

        var (userId, userName) = await CurrentUserAsync();

        var record = new Models.EquipmentInspectionHistory
        {
            EquipmentId = request.EquipmentId,
            Date = request.Date,
            Title = request.Title,
            Content = request.Content,
            Notes = request.Notes,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.EquipmentInspectionHistories.Add(record);
        await db.SaveChangesAsync();
        return Ok(ToDto(record, 0));
    }

    // PUT api/equipment-inspection-history/{id} — EquipmentId는 생성 후 고정이라 요청에 없음.
    // 수정은 시스템관리자 또는 작성자 본인만 가능하다(사용자 결정, 2026-09-16).
    [HttpPut("{id}")]
    public async Task<ActionResult<EquipmentInspectionHistoryDto>> Update(int id, UpdateEquipmentInspectionHistoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("제목과 검사내용은 비어있을 수 없습니다.");

        var record = await db.EquipmentInspectionHistories.FindAsync(id);
        if (record is null)
            return NotFound();

        var (userId, userName) = await CurrentUserAsync();

        if (!IsSystemAdmin && record.CreatedByUserId != userId)
            return Forbid();

        record.Date = request.Date;
        record.Title = request.Title;
        record.Content = request.Content;
        record.Notes = request.Notes;
        record.UpdatedByUserId = userId;
        record.UpdatedByUserName = userName;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var counts = await GetAttachmentCountsAsync([id]);
        return Ok(ToDto(record, counts.GetValueOrDefault(id)));
    }

    // DELETE api/equipment-inspection-history/{id} — 시스템관리자 전용. 딸린 첨부파일(파일+메타데이터)도 같이 정리한다.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await db.EquipmentInspectionHistories.FindAsync(id);
        if (record is null)
            return NotFound();

        var attachments = await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.EquipmentInspectionHistory && a.OwnerId == id)
            .ToListAsync();
        foreach (var a in attachments)
            storage.Delete(a.StoredPath);
        db.Attachments.RemoveRange(attachments);

        db.EquipmentInspectionHistories.Remove(record);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private bool IsSystemAdmin => User.IsSystemAdmin();

    private int CurrentUserId => User.GetUserId();

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
            .Where(a => a.OwnerType == AttachmentOwnerType.EquipmentInspectionHistory && idList.Contains(a.OwnerId))
            .GroupBy(a => a.OwnerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    // CanEdit은 요청한 사용자 기준으로 계산하므로 static이 아니다(Update의 권한 체크와 같은 조건).
    private EquipmentInspectionHistoryDto ToDto(Models.EquipmentInspectionHistory h, int attachmentCount) => new(
        h.Id, h.EquipmentId, h.Date, h.Title, h.Content, h.Notes,
        h.CreatedByUserName, h.CreatedAt, h.UpdatedByUserName, h.UpdatedAt, attachmentCount,
        IsSystemAdmin || h.CreatedByUserId == CurrentUserId);
}
