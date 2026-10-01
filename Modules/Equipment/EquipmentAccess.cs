using HRMS.Modules.Auth;
using System.Security.Claims;
using HRMS.Infrastructure;
using HRMS.Modules.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.Equipment;

//--------------------------------------------------------------------------------//
// 장비관리 영역(장비/압축기/채널 설정 수정) 전용 접근 제어. 시스템관리자는 전체 장비를
// 관리할 수 있고, 그 외 역할은 UserEquipment에 자신이 등록된 장비만 관리할 수 있다.
// 2026-09-28부터 점검일지/운전일지 저장(PUT)에도 같은 기준을 적용한다 — 두 API 모두
// 전체 교체 방식이라, 담당이 아닌 사용자가 남의 장비 일지를 통째로 덮어쓸 수 있었다.
// 수리일지/교육훈련/검사이력/공지사항은 작성자 기준이라 이 헬퍼를 쓰지 않는다.
//--------------------------------------------------------------------------------//
public static class EquipmentAccess
{
    public static async Task<bool> CanManageAsync(AppDbContext db, ClaimsPrincipal user, int equipmentId)
    {
        if (!user.TryGetUser(out int userId, out var role))
            return false;

        if (role == UserRole.시스템관리자)
            return true;

        return await db.UserEquipments.AnyAsync(ue => ue.UserId == userId && ue.EquipmentId == equipmentId);
    }
}
