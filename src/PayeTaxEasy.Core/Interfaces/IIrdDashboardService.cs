using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface IIrdDashboardService
{
    Task<ComplianceReportDto> GetComplianceReportAsync(string financialYear);
    Task<byte[]> ExportComplianceReportAsync(string financialYear);
    Task<EmployerDetailDto> GetEmployerDetailAsync(string registrationNumber);
    Task<PagedResult<AuditLogEntryDto>> GetAuditLogsAsync(AuditLogQueryDto query);
}
