using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;

namespace PayeTaxEasy.Infrastructure.Services;

public class IrdDashboardService : IIrdDashboardService
{
    private readonly PayeTaxEasyDbContext _db;
    private readonly IAuditService _audit;

    public IrdDashboardService(PayeTaxEasyDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ComplianceReportDto> GetComplianceReportAsync(string financialYear)
    {
        int total = await _db.Employers.CountAsync();
        var submitted = await _db.PayrollSubmissions
            .Where(s => s.FinancialYear == financialYear && s.Status == "Accepted")
            .Select(s => s.EmployerId).Distinct().CountAsync();
        decimal totalPaye = await _db.PayrollSubmissions
            .Where(s => s.FinancialYear == financialYear && s.Status == "Accepted")
            .SumAsync(s => s.TotalPAYEAmount);

        return new ComplianceReportDto(financialYear, total, submitted, total - submitted, totalPaye);
    }

    public async Task<byte[]> ExportComplianceReportAsync(string financialYear)
    {
        var report = await GetComplianceReportAsync(financialYear);
        var csv = $"Financial Year,Total Employers,Submitted,Not Submitted,Total PAYE\n" +
                  $"{report.FinancialYear},{report.TotalRegisteredEmployers}," +
                  $"{report.SubmittedCount},{report.NotSubmittedCount},{report.TotalPAYECollected}";
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    public async Task<EmployerDetailDto> GetEmployerDetailAsync(string registrationNumber)
    {
        var employer = await _db.Employers
            .FirstOrDefaultAsync(e => e.RegistrationNumber == registrationNumber)
            ?? throw new KeyNotFoundException($"Employer {registrationNumber} not found.");

        return new EmployerDetailDto(employer.Id, employer.TIN, employer.OrganizationName,
            employer.RegistrationNumber, employer.ContactEmail, employer.SMSNotificationsEnabled);
    }

    public async Task<PagedResult<AuditLogEntryDto>> GetAuditLogsAsync(AuditLogQueryDto query)
    {
        var q = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(query.EmployerTin))
            q = q.Where(a => a.ActorId == query.EmployerTin || a.AdditionalData!.Contains(query.EmployerTin));
        if (!string.IsNullOrEmpty(query.EmployeeTin))
            q = q.Where(a => a.EntityId == query.EmployeeTin);
        if (query.FromDate.HasValue)
            q = q.Where(a => a.Timestamp >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(a => a.Timestamp <= query.ToDate.Value);
        if (!string.IsNullOrEmpty(query.ActionType))
            q = q.Where(a => a.Action == query.ActionType);

        int total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogEntryDto(
                a.Id, a.ActorId, a.ActorRole, a.Action,
                a.EntityType, a.EntityId, a.FinancialYear, a.Timestamp))
            .ToListAsync();

        return new PagedResult<AuditLogEntryDto>(items, total, query.Page, query.PageSize);
    }
}
