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

    public async Task<IEnumerable<EmployerSubmissionSummaryDto>> GetAllEmployersWithSubmissionsAsync(string financialYear)
    {
        var employers = await _db.Employers.ToListAsync();
        var submissions = await _db.PayrollSubmissions
            .Where(s => s.FinancialYear == financialYear && s.Status == "Accepted")
            .ToListAsync();
        var payrolls = await _db.EmployeePayrolls.ToListAsync();

        return employers.Select(emp =>
        {
            var empSubs = submissions.Where(s => s.EmployerId == emp.Id).ToList();
            var empPayrolls = payrolls.Where(p => p.EmployerId == emp.Id && p.IsActive).ToList();
            var latest = empSubs.OrderByDescending(s => s.SubmittedAt).FirstOrDefault();

            return new EmployerSubmissionSummaryDto(
                emp.TIN, emp.OrganizationName, emp.RegistrationNumber,
                empSubs.Count,
                empSubs.Sum(s => s.TotalPAYEAmount),
                empPayrolls.Count,
                latest?.IRDReferenceNumber ?? "—",
                latest?.SubmittedAt);
        }).OrderBy(e => e.OrganizationName);
    }

    public async Task<IEnumerable<EmployeeTaxSummaryDto>> GetAllEmployeesWithTaxAsync(string financialYear)
    {
        var employees = await _db.Employees.ToListAsync();
        var payrolls = await _db.EmployeePayrolls
            .Include(p => p.Employer)
            .Include(p => p.MonthlyDeductions)
            .ToListAsync();
        var irdCaches = await _db.IrdCumulativeCaches
            .Where(c => c.FinancialYear == financialYear)
            .ToListAsync();

        return employees.Select(emp =>
        {
            var activePayroll = payrolls
                .Where(p => p.EmployeeId == emp.Id && p.IsActive)
                .OrderByDescending(p => p.EmploymentStartDate)
                .FirstOrDefault();

            var allPayrolls = payrolls.Where(p => p.EmployeeId == emp.Id).ToList();
            var allDeductions = allPayrolls.SelectMany(p => p.MonthlyDeductions).ToList();
            decimal totalTaxPaid = allDeductions.Sum(d => d.MonthlyDeductionAmount);

            var irdData = irdCaches
                .Where(c => c.EmployeeTIN == emp.TIN)
                .OrderByDescending(c => c.CumulativeDeduction)
                .FirstOrDefault();
            bool hasPrior = irdData != null && irdData.CumulativeDeduction > 0;

            decimal salary = activePayroll?.GrossMonthlySalary ?? 0;
            decimal annualTax = PayeTaxEasy.Core.Calculator.PayeCalculator.CalculateAnnualTax(salary * 12);
            decimal stdMonthly = annualTax > 0 ? Math.Round(annualTax / 12, 0) : 0;

            return new EmployeeTaxSummaryDto(
                emp.TIN, emp.FullName,
                activePayroll?.Employer?.OrganizationName ?? "—",
                salary, annualTax, totalTaxPaid, stdMonthly,
                activePayroll?.EmploymentStartDate ?? DateTime.MinValue,
                hasPrior);
        }).OrderBy(e => e.EmployeeName);
    }
}
