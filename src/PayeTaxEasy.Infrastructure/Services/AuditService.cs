using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly PayeTaxEasyDbContext _db;

    public AuditService(PayeTaxEasyDbContext db) => _db = db;

    public async Task RecordAsync(
        string actorId, string actorRole, string action,
        string entityType, string entityId,
        string? financialYear = null, string? ipAddress = null, string? additionalData = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            FinancialYear = financialYear,
            IPAddress = ipAddress,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
