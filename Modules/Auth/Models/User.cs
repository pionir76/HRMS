namespace HRMS.Modules.Auth.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } // 안전관리 역할 (시스템관리자/안전관리총괄자/안전관리책임자/안전관리원/일반관리자)

    // 비상정지는 관리자 권한과 별도로 분리해서 부여할 수 있는 독립 권한이다 (overview.md 6장).
    public bool CanEmergencyStop { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    //----------------------------------------------------------------------------------//
    // 안전관리 담당자 인적사항. 시스템 로그인 계정 정보와는 별개로, 조직 관리 목적의 정보다.
    //----------------------------------------------------------------------------------//
    public required string FullName { get; set; } // 성명
    public string? Position { get; set; } // 직책 (예: 사장)
    public DateOnly? LegalTrainingDate { get; set; } // 법정교육일
    public DateOnly? NextTrainingDate { get; set; } // 차기교육일
    public string? Department { get; set; } // 부서
    public string? BackupPersonName { get; set; } // 대직자 — 시스템 계정이 아닌 이름 텍스트로만 기록
}
