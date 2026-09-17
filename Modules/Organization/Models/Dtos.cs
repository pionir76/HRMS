namespace HRMS.Modules.Organization.Models;

//--------------------------------------------------------------------------------//
// 조직관리 조회 응답. 비밀번호(해시)는 절대 내려주지 않는다.
// Role = 담당업무(안전관리총괄자/안전관리책임자/안전관리원/일반관리원). 본인 조회(/me)에서만
//        시스템관리자도 나올 수 있다.
// EquipmentIds = 담당장비 Id 목록(장비 정보는 GET /api/equipments로 매칭).
// AppointmentReportId = 선해임 신고서 첨부파일 Id(없으면 null). 파일은 GET /api/attachments/{id}로 받는다.
//--------------------------------------------------------------------------------//
public record OrgUserDto(
    int Id,
    string Username,
    string FullName,
    string? Department,
    string? Position,
    string Role,
    string? Phone1,
    string? Phone2,
    string? BackupPersonName,
    bool IsAppointed,
    DateOnly? LegalTrainingDate,
    DateOnly? NextTrainingDate,
    bool IsActive,
    List<int> EquipmentIds,
    int? AppointmentReportId);

// POST 요청 본문(시스템관리자 전용). 사용자 ID는 생성 후 바꿀 수 없다.
public record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    string? Department,
    string? Position,
    string Role,
    string? Phone1,
    string? Phone2,
    string? BackupPersonName,
    bool IsAppointed,
    DateOnly? LegalTrainingDate,
    DateOnly? NextTrainingDate,
    List<int> EquipmentIds);

// PUT 요청 본문(시스템관리자 전용). 사용자 ID·비밀번호는 여기서 바꾸지 않는다(비밀번호는 전용 API).
public record UpdateUserRequest(
    string FullName,
    string? Department,
    string? Position,
    string Role,
    string? Phone1,
    string? Phone2,
    string? BackupPersonName,
    bool IsAppointed,
    DateOnly? LegalTrainingDate,
    DateOnly? NextTrainingDate,
    List<int> EquipmentIds);

public record ResetPasswordRequest(string NewPassword);

public record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateMyContactRequest(string? Phone1, string? Phone2);

public record UsernameCheckResponse(string Username, bool Available);
