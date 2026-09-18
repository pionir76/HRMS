using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Organization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserEntity = HRMS.Modules.Auth.Models.User;

namespace HRMS.Modules.Organization.Controllers;

//--------------------------------------------------------------------------------//
// 조직관리 API (사용자 등록·조회·수정·비활성화). 사용자 엔티티 자체는 Modules/Auth에 있다.
//
// 권한 규칙(사용자 결정 2026-09-17):
//   - 조회: 로그인한 누구나
//   - 등록·수정·비밀번호 초기화·비활성화: 시스템관리자만
//   - 본인: 자기 비밀번호와 연락처(1·2)만 바꿀 수 있다(/me 엔드포인트)
//
// 담당업무 = Role. 이 API로 지정할 수 있는 값은 안전관리총괄자/안전관리책임자/안전관리원/일반관리원
// 네 가지뿐이다. 시스템관리자는 담당업무가 아니라 별도 관리 계정이라 목록에서 빠지고, 이 API로
// 수정·비활성화할 수도 없다(관리자 계정이 실수로 잠기는 것을 막는 효과도 있다).
//
// 삭제는 없다 — 비활성화(IsActive=false)만 한다. 결재·작성 기록과 담당장비 기록을 보존하기 위함.
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext db) : ControllerBase
{
    private static readonly PasswordHasher<UserEntity> Hasher = new();

    private static readonly UserRole[] AssignableRoles =
        [UserRole.안전관리총괄자, UserRole.안전관리책임자, UserRole.안전관리원, UserRole.일반관리원];

    // GET api/users — 조직 구성원 전체(비활성 포함, 시스템관리자 제외). 담당업무 순 → 성명 순.
    [HttpGet]
    public async Task<ActionResult<List<OrgUserDto>>> GetList()
    {
        var users = await db.Users
            .Where(u => u.Role != UserRole.시스템관리자)
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .ToListAsync();

        return Ok(await ToDtosAsync(users));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrgUserDto>> GetOne(int id)
    {
        var user = await FindOrgUserAsync(id);
        if (user is null)
            return NotFound();

        return Ok((await ToDtosAsync([user]))[0]);
    }

    // GET api/users/me — 로그인한 본인 정보(시스템관리자 포함). 본인 정보 수정 화면용.
    [HttpGet("me")]
    public async Task<ActionResult<OrgUserDto>> GetMe()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            return NotFound();

        return Ok((await ToDtosAsync([user]))[0]);
    }

    // GET api/users/check-username?username= — 사용자 ID 중복확인(비활성 계정의 ID도 사용 중으로 본다).
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpGet("check-username")]
    public async Task<ActionResult<UsernameCheckResponse>> CheckUsername([FromQuery] string username)
    {
        var name = username?.Trim() ?? "";
        if (name.Length == 0)
            return BadRequest("사용자 ID를 입력하세요.");

        var taken = await db.Users.AnyAsync(u => u.Username == name);
        return Ok(new UsernameCheckResponse(name, !taken));
    }

    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost]
    public async Task<ActionResult<OrgUserDto>> Create(CreateUserRequest request)
    {
        var username = request.Username?.Trim() ?? "";
        if (username.Length == 0)
            return BadRequest("사용자 ID를 입력하세요.");
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("비밀번호를 입력하세요.");

        var error = ValidateCommon(request.FullName, request.Role, out var role);
        if (error is not null)
            return BadRequest(error);

        var equipmentIds = request.EquipmentIds?.Distinct().ToList() ?? [];
        if (!await AllEquipmentsExistAsync(equipmentIds))
            return BadRequest("존재하지 않는 장비가 포함되어 있습니다.");

        if (await db.Users.AnyAsync(u => u.Username == username))
            return Conflict("이미 사용 중인 사용자 ID입니다.");

        var user = new UserEntity
        {
            Username = username,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            FullName = request.FullName.Trim(),
        };
        ApplyProfile(user, request.Department, request.Position, request.Phone1, request.Phone2,
            request.BackupPersonName, request.IsAppointed, request.LegalTrainingDate, request.NextTrainingDate);
        user.PasswordHash = Hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserEquipments.AddRange(equipmentIds.Select(eid => new UserEquipment { UserId = user.Id, EquipmentId = eid }));
        await db.SaveChangesAsync();

        return Ok((await ToDtosAsync([user]))[0]);
    }

    // PUT api/users/{id} — 인적사항·담당업무·담당장비 수정. 담당장비는 요청 목록으로 통째로 교체한다.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrgUserDto>> Update(int id, UpdateUserRequest request)
    {
        var user = await FindOrgUserAsync(id);
        if (user is null)
            return NotFound();

        var error = ValidateCommon(request.FullName, request.Role, out var role);
        if (error is not null)
            return BadRequest(error);

        var equipmentIds = request.EquipmentIds?.Distinct().ToList() ?? [];
        if (!await AllEquipmentsExistAsync(equipmentIds))
            return BadRequest("존재하지 않는 장비가 포함되어 있습니다.");

        user.FullName = request.FullName.Trim();
        user.Role = role;
        ApplyProfile(user, request.Department, request.Position, request.Phone1, request.Phone2,
            request.BackupPersonName, request.IsAppointed, request.LegalTrainingDate, request.NextTrainingDate);

        var current = await db.UserEquipments.Where(ue => ue.UserId == id).ToListAsync();
        db.UserEquipments.RemoveRange(current.Where(ue => !equipmentIds.Contains(ue.EquipmentId)));
        var existingIds = current.Select(ue => ue.EquipmentId).ToHashSet();
        db.UserEquipments.AddRange(equipmentIds.Where(eid => !existingIds.Contains(eid))
            .Select(eid => new UserEquipment { UserId = id, EquipmentId = eid }));

        await db.SaveChangesAsync();
        return Ok((await ToDtosAsync([user]))[0]);
    }

    // PUT api/users/{id}/password — 시스템관리자가 비밀번호를 새로 지정(초기화)한다.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("비밀번호를 입력하세요.");

        var user = await FindOrgUserAsync(id);
        if (user is null)
            return NotFound();

        user.PasswordHash = Hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/users/{id}/deactivate — 비활성화(로그인 불가). 삭제 대신 쓴다.
    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<OrgUserDto>> Deactivate(int id) => await SetActiveAsync(id, false);

    [Authorize(Roles = nameof(UserRole.시스템관리자))]
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<OrgUserDto>> Activate(int id) => await SetActiveAsync(id, true);

    // PUT api/users/me/password — 본인 비밀번호 변경. 현재 비밀번호 확인이 필요하다(시스템관리자 포함).
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword(ChangeMyPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("새 비밀번호를 입력하세요.");

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            return NotFound();

        if (Hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword ?? "") == PasswordVerificationResult.Failed)
            return BadRequest("현재 비밀번호가 올바르지 않습니다.");

        user.PasswordHash = Hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PUT api/users/me/contact — 본인 연락처 변경(본인이 바꿀 수 있는 인적사항은 연락처뿐이다).
    [HttpPut("me/contact")]
    public async Task<ActionResult<OrgUserDto>> UpdateMyContact(UpdateMyContactRequest request)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null)
            return NotFound();

        user.Phone1 = Normalize(request.Phone1);
        user.Phone2 = Normalize(request.Phone2);
        await db.SaveChangesAsync();
        return Ok((await ToDtosAsync([user]))[0]);
    }

    private async Task<ActionResult<OrgUserDto>> SetActiveAsync(int id, bool active)
    {
        var user = await FindOrgUserAsync(id);
        if (user is null)
            return NotFound();

        user.IsActive = active;
        await db.SaveChangesAsync();
        return Ok((await ToDtosAsync([user]))[0]);
    }

    // 조직관리 대상 사용자만 찾는다 — 시스템관리자 계정은 없는 것으로 취급(404).
    private Task<UserEntity?> FindOrgUserAsync(int id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role != UserRole.시스템관리자);

    private static string? ValidateCommon(string? fullName, string? roleText, out UserRole role)
    {
        role = default;
        if (string.IsNullOrWhiteSpace(fullName))
            return "성명을 입력하세요.";
        if (!Enum.TryParse(roleText, out role) || !AssignableRoles.Contains(role))
            return "담당업무는 안전관리총괄자/안전관리책임자/안전관리원/일반관리원 중 하나여야 합니다.";
        return null;
    }

    private static void ApplyProfile(UserEntity user, string? department, string? position,
        string? phone1, string? phone2, string? backupPersonName, bool isAppointed,
        DateOnly? legalTrainingDate, DateOnly? nextTrainingDate)
    {
        user.Department = Normalize(department);
        user.Position = Normalize(position);
        user.Phone1 = Normalize(phone1);
        user.Phone2 = Normalize(phone2);
        user.BackupPersonName = Normalize(backupPersonName);
        user.IsAppointed = isAppointed;
        user.LegalTrainingDate = legalTrainingDate;
        user.NextTrainingDate = nextTrainingDate;
    }

    // 빈 문자열은 null로 저장한다(화면에서 지운 값과 원래 없던 값을 같게 취급).
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> AllEquipmentsExistAsync(List<int> equipmentIds)
    {
        if (equipmentIds.Count == 0)
            return true;
        var count = await db.Equipments.CountAsync(e => equipmentIds.Contains(e.Id));
        return count == equipmentIds.Count;
    }

    private int CurrentUserId => User.GetUserId();

    // 담당장비·신고서를 사용자별로 한 번에 모아서 붙인다(사용자 수만큼 쿼리가 늘지 않게).
    private async Task<List<OrgUserDto>> ToDtosAsync(List<UserEntity> users)
    {
        var ids = users.Select(u => u.Id).ToList();

        var equipmentByUser = (await db.UserEquipments
                .Where(ue => ids.Contains(ue.UserId))
                .ToListAsync())
            .GroupBy(ue => ue.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ue => ue.EquipmentId).OrderBy(x => x).ToList());

        var reportByUser = await db.Attachments
            .Where(a => a.OwnerType == AttachmentOwnerType.AppointmentReport && ids.Contains(a.OwnerId))
            .ToDictionaryAsync(a => a.OwnerId, a => a.Id);

        return users.Select(u => new OrgUserDto(
            u.Id, u.Username, u.FullName, u.Department, u.Position, u.Role.ToString(),
            u.Phone1, u.Phone2, u.BackupPersonName, u.IsAppointed,
            u.LegalTrainingDate, u.NextTrainingDate, u.IsActive,
            equipmentByUser.GetValueOrDefault(u.Id) ?? [],
            reportByUser.TryGetValue(u.Id, out var rid) ? rid : null)).ToList();
    }
}
