namespace HRMS.Modules.Auth.Models;

//-----------------------------------------------------------------------------//
// 사용자 계정 정보. 시스템 로그인 계정과 안전관리 담당자 인적사항을 함께 관리한다.
// 등록/수정은 조직관리 API(Modules/Organization)가 담당한다.
// Role은 조직관리 화면의 "담당업무"다(안전관리총괄자/안전관리책임자/안전관리원/일반관리원).
// 삭제하지 않고 IsActive=false로 비활성화한다 — 결재·작성 이력의 이름 스냅샷과 담당장비 기록을 보존하기 위함.
//-----------------------------------------------------------------------------//
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = ""; // 사용자 ID (유니크)
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } // 담당업무

    public bool IsActive { get; set; } = true; // false면 로그인 불가(비활성화)
    public DateTimeOffset CreatedAt { get; set; }

    //----------------------------------------------------------------------------------//
    // 안전관리 담당자 인적사항. 시스템 로그인 계정 정보와는 별개로, 조직 관리 목적의 정보다.
    //----------------------------------------------------------------------------------//
    public required string FullName { get; set; } // 성명
    public string? Department { get; set; } // 소속팀
    public string? Position { get; set; } // 직위 (단순 문자열, 예: 과장)
    public string? Phone1 { get; set; } // 연락처1
    public string? Phone2 { get; set; } // 연락처2
    public string? BackupPersonName { get; set; } // 대직자 — 시스템 계정이 아닌 이름 텍스트로만 기록
    public bool IsAppointed { get; set; } // 선임여부 (true=선임, false=미선임)
    public DateOnly? LegalTrainingDate { get; set; } // 법정교육일
    public DateOnly? NextTrainingDate { get; set; } // 차기교육일

    // 선해임 신고서 파일은 Modules/Attachment(ownerType=AppointmentReport, ownerId=이 Id)로 관리한다.
    // 담당장비 목록은 UserEquipment(다대다)로 관리한다.
}
