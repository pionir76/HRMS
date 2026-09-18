using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Attachment.Controllers;

//-----------------------------------------------------------------------------//
// 첨부파일 공용 API. OwnerType으로 도메인을 구분하고, 변경 권한은 두 갈래로 나뉜다:
//   - EquipmentPhoto(장비 사진): 담당 장비 기준(EquipmentAccess). jpg/png만, 슬롯당 1장(덮어쓰기)
//   - 문서형(EquipmentInspectionHistory / TrainingLog / Notice): **그 문서를 수정할 수 있는
//     사람만**(작성자 또는 시스템관리자) 첨부를 추가/삭제할 수 있다(CheckDocumentOwnerAsync).
//     확장자 제한 없고 슬롯도 쓰지 않는다(다건 첨부). 교육훈련은 결재가 진행되면 잠긴다.
//   - AppointmentReport(선해임 신고서): 시스템관리자만, 사용자당 1건(서버가 슬롯 "report"를
//     강제해서 재업로드 시 교체), 확장자 제한 없음(CheckAppointmentReportOwnerAsync)
// 파일 용량 제한(10MB)은 첨부파일 공통 규칙이라 OwnerType 상관없이 전부 적용한다(api-manual.md 첨부파일 절).
// 조회(GET)는 어느 OwnerType이든 로그인만 요구한다.
//-----------------------------------------------------------------------------//
[ApiController]
[Route("api/attachments")]
[Authorize]
public class AttachmentsController(AppDbContext db, AttachmentStorage storage) : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] EquipmentPhotoSlots = ["equipment", "installation"];
    private const string AppointmentReportSlot = "report";

    [HttpGet]
    public async Task<ActionResult<List<AttachmentDto>>> GetList([FromQuery] string ownerType, [FromQuery] int ownerId)
    {
        if (!Enum.TryParse<AttachmentOwnerType>(ownerType, out var type))
            return BadRequest("올바르지 않은 ownerType입니다.");

        var list = await db.Attachments
            .Where(a => a.OwnerType == type && a.OwnerId == ownerId)
            .OrderBy(a => a.Slot).ThenBy(a => a.UploadedAt)
            .ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await db.Attachments.FindAsync(id);
        if (attachment is null) return NotFound();

        var fullPath = storage.GetFullPath(attachment.StoredPath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        return PhysicalFile(fullPath, attachment.ContentType, attachment.FileName);
    }

    [HttpPost]
    public async Task<ActionResult<AttachmentDto>> Upload(
        [FromQuery] string ownerType, [FromQuery] int ownerId, [FromQuery] string? slot, IFormFile file)
    {
        if (!Enum.TryParse<AttachmentOwnerType>(ownerType, out var type))
            return BadRequest("올바르지 않은 ownerType입니다.");
        if (file.Length == 0)
            return BadRequest("빈 파일입니다.");
        if (file.Length > MaxFileSizeBytes)
            return BadRequest("파일 용량은 10MB를 넘을 수 없습니다.");

        if (type == AttachmentOwnerType.EquipmentPhoto)
        {
            // 장비 사진만 다른 규칙이다 — 작성자 개념이 없고 담당 장비 기준이며, 슬롯/확장자 제약이 있다.
            if (!await Equipment.EquipmentAccess.CanManageAsync(db, User, ownerId))
                return Forbid();
            if (await db.Equipments.FindAsync(ownerId) is null)
                return NotFound("존재하지 않는 장비입니다.");
            if (slot is null || !EquipmentPhotoSlots.Contains(slot))
                return BadRequest("slot은 equipment 또는 installation이어야 합니다.");
            if (!AllowedImageExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                return BadRequest("jpg, png 이미지만 업로드할 수 있습니다.");
        }
        else if (type == AttachmentOwnerType.AppointmentReport)
        {
            // 선해임 신고서는 조직관리 정보라 시스템관리자만 바꿀 수 있고, 사용자당 1건으로 고정한다.
            // 슬롯을 서버가 강제로 지정해서 기존 슬롯 덮어쓰기 로직과 유니크 인덱스를 그대로 활용한다.
            var check = await CheckAppointmentReportOwnerAsync(ownerId);
            if (check is not null)
                return check;
            slot = AppointmentReportSlot;
        }
        else
        {
            // 문서형 첨부(검사이력/교육훈련/공지사항)는 확장자 제한 없이 다건이고(slot 미사용),
            // 그 문서를 수정할 수 있는 사람만 첨부를 바꿀 수 있다.
            var check = await CheckDocumentOwnerAsync(type, ownerId);
            if (check is not null)
                return check;
            slot = null;
        }

        var userId = CurrentUserId;
        var userName = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        var storedPath = await storage.SaveAsync(type.ToString(), extension, stream);

        //-----------------------------------------------------------------------------//
        // 슬롯이 있을 때만(EquipmentPhoto) 같은 슬롯의 기존 행을 찾아 덮어쓴다. 슬롯이 없는
        // 다건 첨부(EquipmentInspectionHistory 등)는 매번 새 행을 추가해야 하므로, slot이
        // null이면 "기존 행 찾기" 자체를 하지 않는다(안 하면 같은 OwnerId에 두 번째 파일을
        // 올릴 때 Slot=null인 첫 번째 파일을 찾아서 덮어써버리는 버그가 생긴다).
        //-----------------------------------------------------------------------------//
        var existing = slot is null
            ? null
            : await db.Attachments.FirstOrDefaultAsync(a => a.OwnerType == type && a.OwnerId == ownerId && a.Slot == slot);

        if (existing is not null)
        {
            storage.Delete(existing.StoredPath);
            existing.FileName = file.FileName;
            existing.ContentType = file.ContentType;
            existing.SizeBytes = file.Length;
            existing.StoredPath = storedPath;
            existing.UploadedByUserId = userId;
            existing.UploadedByUserName = userName;
            existing.UploadedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Ok(ToDto(existing));
        }

        var attachment = new Models.Attachment
        {
            OwnerType = type,
            OwnerId = ownerId,
            Slot = slot,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StoredPath = storedPath,
            UploadedByUserId = userId,
            UploadedByUserName = userName,
            UploadedAt = DateTimeOffset.UtcNow
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();
        return Ok(ToDto(attachment));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var attachment = await db.Attachments.FindAsync(id);
        if (attachment is null) return NotFound();

        if (attachment.OwnerType == AttachmentOwnerType.EquipmentPhoto)
        {
            if (!await Equipment.EquipmentAccess.CanManageAsync(db, User, attachment.OwnerId))
                return Forbid();
        }
        else if (attachment.OwnerType == AttachmentOwnerType.AppointmentReport)
        {
            var check = await CheckAppointmentReportOwnerAsync(attachment.OwnerId);
            if (check is not null)
                return check;
        }
        else
        {
            // 업로드와 동일한 조건 — 그 문서를 수정할 수 있는 사람만 첨부를 지울 수 있다.
            var check = await CheckDocumentOwnerAsync(attachment.OwnerType, attachment.OwnerId);
            if (check is not null)
                return check;
        }

        storage.Delete(attachment.StoredPath);
        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    //-----------------------------------------------------------------------------//
    // 문서형 OwnerType(검사이력/교육훈련/공지사항)의 첨부 변경 가능 여부를 부모 문서 기준으로
    // 판정한다. 첨부 추가/삭제도 결국 문서 내용 변경이므로, **그 문서를 수정할 수 있는 사람만**
    // (= 작성자 또는 시스템관리자) 첨부를 바꿀 수 있어야 한다는 사용자 결정에 따른 것이다.
    // 교육훈련은 여기에 "결재가 진행되면 아무도 못 바꾼다"는 잠금 규칙이 추가로 붙는다.
    //
    // 원래 이 모듈은 도메인에 무관하게 동작하도록 만들었지만, 이 규칙을 지키려면 부모 문서를
    // 들여다볼 수밖에 없다(EquipmentPhoto가 EquipmentAccess를 쓰는 것과 같은 성격).
    // 새 문서형 OwnerType을 추가할 때는 이 switch에 작성자/잠금 조회를 같이 추가해야 한다.
    //
    // 반환값: 문제가 없으면 null, 막아야 하면 그에 맞는 ActionResult.
    //-----------------------------------------------------------------------------//
    private async Task<ActionResult?> CheckDocumentOwnerAsync(AttachmentOwnerType type, int ownerId)
    {
        var owner = type switch
        {
            AttachmentOwnerType.EquipmentInspectionHistory => await db.EquipmentInspectionHistories
                .Where(x => x.Id == ownerId)
                .Select(x => new { x.CreatedByUserId, Locked = false })
                .FirstOrDefaultAsync(),
            AttachmentOwnerType.TrainingLog => await db.TrainingLogs
                .Where(x => x.Id == ownerId)
                .Select(x => new
                {
                    x.CreatedByUserId,
                    Locked = x.Level1ApprovedAt != null || x.Level2ApprovedAt != null || x.Level3ApprovedAt != null
                })
                .FirstOrDefaultAsync(),
            AttachmentOwnerType.Notice => await db.Notices
                .Where(x => x.Id == ownerId)
                .Select(x => new { x.CreatedByUserId, Locked = false })
                .FirstOrDefaultAsync(),
            _ => null
        };

        if (owner is null)
            return type is AttachmentOwnerType.EquipmentInspectionHistory or AttachmentOwnerType.TrainingLog or AttachmentOwnerType.Notice
                ? NotFound("존재하지 않는 문서입니다.")
                : BadRequest("아직 지원하지 않는 ownerType입니다.");

        if (owner.Locked)
            return Conflict("결재가 진행된 문서의 첨부파일은 변경할 수 없습니다.");

        if (!IsSystemAdmin && owner.CreatedByUserId != CurrentUserId)
            return Forbid();

        return null;
    }

    //-----------------------------------------------------------------------------//
    // 선해임 신고서(사용자당 1건): 조직관리 정보라 시스템관리자만 올리고 지울 수 있다.
    // ownerId는 사용자 Id이며, 시스템관리자 계정은 조직관리 대상이 아니라 신고서도 받지 않는다.
    //-----------------------------------------------------------------------------//
    private async Task<ActionResult?> CheckAppointmentReportOwnerAsync(int userId)
    {
        if (!IsSystemAdmin)
            return Forbid();

        var role = await db.Users.Where(u => u.Id == userId).Select(u => (UserRole?)u.Role).FirstOrDefaultAsync();
        if (role is null || role == UserRole.시스템관리자)
            return NotFound("존재하지 않는 사용자입니다.");

        return null;
    }

    private bool IsSystemAdmin => User.IsSystemAdmin();

    private int CurrentUserId => User.GetUserId();

    private static AttachmentDto ToDto(Models.Attachment a) => new(
        a.Id, a.OwnerType.ToString(), a.OwnerId, a.Slot, a.FileName, a.ContentType, a.SizeBytes,
        a.UploadedByUserName, a.UploadedAt);
}
