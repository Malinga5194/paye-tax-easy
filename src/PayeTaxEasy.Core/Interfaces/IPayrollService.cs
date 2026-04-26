using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface IPayrollService
{
    Task<SalaryRecordDto> AddSalaryAsync(string employerTin, string employeeTin, decimal grossMonthlySalary, DateTime employmentStartDate, string financialYear);
    Task<SalaryRecordDto> UpdateSalaryAsync(Guid salaryRecordId, decimal newGrossMonthlySalary, DateTime effectiveDate);
    Task<IEnumerable<SalaryRecordDto>> GetSalaryHistoryAsync(string employerTin, string employeeTin);
    Task<IEnumerable<DeductionSummaryDto>> GetDeductionSummaryAsync(string employerTin, string period);
    Task FinalizePeriodAsync(string employerTin, string period);
    Task<SubmissionResultDto> SubmitPayrollAsync(string employerTin, PayrollSubmissionRequestDto request);
    Task<SubmissionResultDto> SubmitBulkPayrollAsync(string employerTin, BulkPayrollSubmissionRequestDto request);
}
