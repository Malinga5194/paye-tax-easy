namespace PayeTaxEasy.Infrastructure.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? FinancialYear { get; set; }
    public string? IPAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AdditionalData { get; set; }
}
