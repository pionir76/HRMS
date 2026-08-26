using HRMS.Infrastructure;
using HRMS.Modules.Logging.Models;

namespace HRMS.Modules.Logging;

// EventLog 저장 헬퍼. 서비스/리포지토리 계층 없이 DbContext를 직접 받아서 한 줄로 기록한다.
public static class EventLogger
{
    public static async Task LogAsync(AppDbContext db, EventLogCategory category, string message, string? username = null)
    {
        db.EventLogs.Add(new EventLog
        {
            Category = category,
            Message = message,
            Username = username,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
