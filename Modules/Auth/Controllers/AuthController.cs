using HRMS.Infrastructure;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Auth.Services;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtTokenService jwt) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    // POST api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
        if (user is null || Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();

        await EventLogger.LogAsync(db, EventLogCategory.UserAccess, $"{user.Username} 로그인", user.Username);

        var token = jwt.CreateToken(user);
        return Ok(new LoginResponse(token, user.Username, user.Role.ToString(), user.CanEmergencyStop));
    }

    // POST api/auth/logout — JWT는 서버가 무효화하지 않는다(클라이언트가 토큰을 버리면 끝). 로그아웃 이력만 남긴다.
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name ?? "unknown";
        await EventLogger.LogAsync(db, EventLogCategory.UserAccess, $"{username} 로그아웃", username);
        return Ok();
    }
}
