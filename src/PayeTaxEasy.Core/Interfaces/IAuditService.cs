namespace PayeTaxEasy.Infrastructure.Services;

public interface IAuditService
{
    Task RecordAsync(string actorId, string actorRole, string action,
        string entityType, string entityId, string? financialYear = null,
        string? ipAddress = null, string? additionalData = null);
}
