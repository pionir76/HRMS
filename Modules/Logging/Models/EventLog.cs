namespace HRMS.Modules.Logging.Models;

public class EventLog
{
    public int Id { get; set; }
    public EventLogCategory Category { get; set; }
    public string Message { get; set; } = "";
    public string? Username { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
