using System.Security.Claims;
using HRMS.Modules.Auth.Models;

namespace HRMS.Modules.Auth;

//--------------------------------------------------------------------------------//
// JWT 클레임에서 로그인 사용자 정보를 꺼내는 공용 헬퍼. 같은 파싱 코드가 컨트롤러 10여 곳에
// 복사돼 있어서 여기로 모았다(2026-09-18 정리).
//
// GetUserId는 [Authorize]가 걸린 API에서만 쓴다 — 토큰이 있는 게 보장되므로 클레임이 없으면
// 그냥 예외(잘못된 토큰 = 버그)다. 클레임이 없을 수 있는 자리에서는 TryGetUser를 쓴다.
//--------------------------------------------------------------------------------//
public static class CurrentUser
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool IsSystemAdmin(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), out var role) && role == UserRole.시스템관리자;

    public static bool TryGetUser(this ClaimsPrincipal user, out int userId, out UserRole role)
    {
        role = default;
        userId = 0;
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = user.FindFirstValue(ClaimTypes.Role);
        return idClaim is not null && roleClaim is not null
            && int.TryParse(idClaim, out userId) && Enum.TryParse(roleClaim, out role);
    }
}
