namespace PayeTaxEasy.Infrastructure.Entities;

public class SecurityAlertLog
{
    public long Id { get; set; }
    public string AttemptedAction { get; set; } = string.Empty;
    public string? AttemptedBy { get; set; }
    public DateTime Timestamp { get; set; }
}
