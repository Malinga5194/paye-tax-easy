namespace PayeTaxEasy.Infrastructure.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string? ReferenceId { get; set; }
}
