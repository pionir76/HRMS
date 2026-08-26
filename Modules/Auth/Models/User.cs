namespace HRMS.Modules.Auth.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }

    // 비상정지는 관리자 권한과 별도로 분리해서 부여할 수 있는 독립 권한이다 (overview.md 6장).
    public bool CanEmergencyStop { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
