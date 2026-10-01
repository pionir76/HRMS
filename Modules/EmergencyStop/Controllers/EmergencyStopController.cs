using HRMS.Infrastructure;
using HRMS.Modules.Auth;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.AspNetCore.Identity;
using HRMS.Modules.EmergencyStop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Modules.EmergencyStop.Controllers;

//--------------------------------------------------------------------------------//
// 비상정지 API. 장비 단위이며(압축기 한 대만 멈추는 것은 하드웨어적으로 불가능 —
// EmergencyStopService 주석 참고), 정지와 해제가 별도 엔드포인트다. 같은 경로에 bool을
// 받는 대신 경로를 나눈 이유는, 되돌리기 어려운 동작이라 호출부가 무엇을 하는지 URL에서
// 분명히 드러나는 편이 안전해서다.
//
// 권한: 시스템관리자 / 안전관리총괄자 / 안전관리책임자 (사용자 결정 2026-09-29).
// 담당 장비(UserEquipment) 여부는 보지 않고 역할만 본다 — 비상 상황에서 담당이 아니라는
// 이유로 막으면 위험하다. 해제도 정지와 같은 권한이다(사용자 결정).
//
// 사용자 재확인(오조작 방지)은 프론트 몫이다. 서버는 같은 장비에 명령이 겹쳐 들어오는 것만
// 막는다(EmergencyStopService의 장비별 잠금).
//--------------------------------------------------------------------------------//
[ApiController]
[Route("api/equipments/{id}/emergency-stop")]
[Authorize(Roles = $"{nameof(UserRole.시스템관리자)},{nameof(UserRole.안전관리총괄자)},{nameof(UserRole.안전관리책임자)}")]
public class EmergencyStopController(EmergencyStopService service, AppDbContext db) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    //--------------------------------------------------------------------------------//
    // POST api/equipments/{id}/emergency-stop — 비상정지 실행(D1805에 0001)
    //
    // 본문의 password로 **로그인한 본인의 비밀번호를 다시 확인**한 뒤에만 명령을 보낸다
    // (프론트 요청 2026-09-29 — 오조작 방지용 재확인을 서버에서도 강제). 틀리면 401이 아니라
    // 400이다 — 401을 받으면 프론트가 토큰 만료로 보고 로그아웃시키기 때문이다.
    // 해제(76번)는 비밀번호를 받지 않는다(프론트 요청).
    //--------------------------------------------------------------------------------//
    [HttpPost]
    public async Task<ActionResult<EmergencyStopResponse>> Stop(
        int id, EmergencyStopRequest? request, CancellationToken cancellationToken)
    {
        var equipment = await db.Equipments.FindAsync([id], cancellationToken);
        if (equipment is null)
            return NotFound();

        var user = await db.Users.FindAsync([User.GetUserId()], cancellationToken);
        if (user is null)
            return Unauthorized();

        if (string.IsNullOrEmpty(request?.Password) ||
            Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            //--------------------------------------------------------------------------------//
            // 거부도 이력으로 남긴다 — 누가 어느 장비를 멈추려다 막혔는지는 비상정지 감사에서
            // 성공만큼 중요하다. 명령은 보내지 않았으므로 압축기는 비워 둔다.
            //--------------------------------------------------------------------------------//
            await EventLogger.LogAsync(db, EventLogCategory.EmergencyStop,
                $"{user.Username}님이 {equipment.BuildingName} {equipment.Name}에 비상정지를 시도했으나 비밀번호 불일치로 거부했습니다.",
                user.Username, equipment.Id);

            return BadRequest(new { message = "비밀번호가 올바르지 않습니다." });
        }

        return await ExecuteAsync(id, stop: true, cancellationToken);
    }

    // POST api/equipments/{id}/emergency-stop/release — 비상정지 해제(D1805에 0000)
    [HttpPost("release")]
    public Task<ActionResult<EmergencyStopResponse>> Release(int id, CancellationToken cancellationToken) =>
        ExecuteAsync(id, stop: false, cancellationToken);

    //--------------------------------------------------------------------------------//
    // 통신이 끊긴 장비여도 막지 않고 그대로 시도한다(사용자 결정 2026-09-29). 통신 상태는
    // 최대 3초 전 정보라 지금은 살아있을 수 있고, 비상 상황에서 서버가 지레 막는 것이 더 위험하다.
    // 실패하면 사유를 그대로 돌려준다.
    //
    // 명령을 보냈는데 실패한 경우도 200이 아니라 409로 준다 — 프론트가 성공/실패를 본문
    // 파싱 없이 구분할 수 있어야 한다(되돌리기 어려운 동작이라 더 분명해야 한다).
    //--------------------------------------------------------------------------------//
    private async Task<ActionResult<EmergencyStopResponse>> ExecuteAsync(int id, bool stop, CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(id, stop, User.Identity?.Name, cancellationToken);
        if (result.EquipmentNotFound)
            return NotFound();

        var response = new EmergencyStopResponse(
            id, result.Succeeded, result.IsEmergencyStopped, result.Confirmed, result.Message);

        return result.Succeeded ? Ok(response) : Conflict(response);
    }
}

//--------------------------------------------------------------------------------//
// isEmergencyStopped는 명령 후의 장비 상태다. confirmed는 명령 직후 D1805를 되읽어 실제로
// 바뀐 것까지 확인했는지 — false여도 명령 자체는 성공이며, 폴링이 3초 안에 상태를 맞춘다.
//--------------------------------------------------------------------------------//
// 75번(정지) 요청 본문. 로그인한 본인의 비밀번호.
public record EmergencyStopRequest(string? Password);

public record EmergencyStopResponse(
    int EquipmentId, bool Succeeded, bool IsEmergencyStopped, bool Confirmed, string Message);
