using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment;

//--------------------------------------------------------------------------------//
// 장비관리 영역(장비/압축기/채널 설정 수정) 전용 접근 제어. 시스템관리자는 전체 장비를
// 관리할 수 있고, 그 외 역할은 UserEquipment에 자신이 등록된 장비만 관리할 수 있다.
// 다른 API(점검일지/운전일지 저장 등)에는 이 제약을 적용하지 않는다 — 그쪽은 사양이
// 아직 정해지지 않아 보류 중이다(사용자 결정).
//--------------------------------------------------------------------------------//
public static class EquipmentAccess
{
    public static async Task<bool> CanManageAsync(AppDbContext db, ClaimsPrincipal user, int equipmentId)
    {
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = user.FindFirstValue(ClaimTypes.Role);
        if (idClaim is null || roleClaim is null || !int.TryParse(idClaim, out var userId) || !Enum.TryParse<UserRole>(roleClaim, out var role))
            return false;

        if (role == UserRole.시스템관리자)
            return true;

        return await db.UserEquipments.AnyAsync(ue => ue.UserId == userId && ue.EquipmentId == equipmentId);
    }
}
