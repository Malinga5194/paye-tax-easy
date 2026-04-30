namespace PayeTaxEasy.Core.Models;

public record SalaryRecordDto(
    Guid SalaryRecordId,
    string EmployeeTIN,
    decimal GrossMonthlySalary,
    decimal AnnualTaxLiability,
    decimal MonthlyDeduction,
    DateTime EffectiveDate,
    string FinancialYear);

public record DeductionSummaryDto(
    string EmployeeTIN,
    string EmployeeName,
    decimal GrossSalary,
    decimal MonthlyDeduction,
    decimal YearToDateCumulativeDeduction,
    bool HasPriorEmployerData,
    bool IsOverpaid,
    decimal AnnualTaxLiability = 0,
    decimal RemainingTaxForYear = 0,
    int RemainingMonthsInFY = 0,
    string Scenario = "",
    DateTime? JoiningDate = null);

public record PayrollSubmissionRequestDto(
    string FinancialYear,
    string Period,
    IEnumerable<SubmissionLineDto> Lines);

public record BulkPayrollSubmissionRequestDto(
    string FinancialYear,
    IEnumerable<SubmissionLineDto> Lines);

public record SubmissionLineDto(
    string EmployeeTIN,
    decimal GrossSalary,
    decimal MonthlyDeduction,
    int Month,
    int Year);

public record SubmissionResultDto(
    Guid SubmissionId,
    string? IrdReferenceNumber,
    string Status,
    decimal TotalPAYEAmount,
    DateTime SubmittedAt);

public record CumulativeDataDto(
    string EmployeeTIN,
    string FinancialYear,
    decimal CumulativeIncome,
    decimal CumulativeDeduction,
    DateTime RetrievedAt,
    string Source);

public record IrdSubmissionAckDto(
    string ReferenceNumber,
    string Status,
    DateTime AcceptedAt);

public record DeductionHistoryDto(
    string EmployeeTIN,
    string EmployeeFullName,
    string FinancialYear,
    IEnumerable<DeductionEntryDto> Entries,
    decimal CumulativeTotal);

public record DeductionEntryDto(
    string EmployerName,
    decimal DeductionAmount,
    DateTime DeductionDate,
    int Month,
    int Year);

public record ComplianceReportDto(
    string FinancialYear,
    int TotalRegisteredEmployers,
    int SubmittedCount,
    int NotSubmittedCount,
    decimal TotalPAYECollected);

public record EmployerDetailDto(
    Guid Id,
    string TIN,
    string OrganizationName,
    string RegistrationNumber,
    string ContactEmail,
    bool SMSNotificationsEnabled);

public record AuditLogEntryDto(
    long Id,
    string ActorId,
    string ActorRole,
    string Action,
    string EntityType,
    string EntityId,
    string? FinancialYear,
    DateTime Timestamp);

public record AuditLogQueryDto(
    string? EmployerTin,
    string? EmployeeTin,
    DateTime? FromDate,
    DateTime? ToDate,
    string? ActionType,
    int Page = 1,
    int PageSize = 50);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record SubmissionConfirmedPayload(
    string ReferenceNumber,
    DateTime SubmittedAt,
    decimal TotalPAYEAmount);

public record SubmissionFailedPayload(
    string ErrorCode,
    string ErrorMessage,
    IEnumerable<string> ValidationErrors);

public record EmployerSubmissionSummaryDto(
    string EmployerTIN,
    string OrganizationName,
    string RegistrationNumber,
    int TotalSubmissions,
    decimal TotalPAYESubmitted,
    int EmployeeCount,
    string LatestSubmissionRef,
    DateTime? LatestSubmissionDate);

public record EmployeeTaxSummaryDto(
    string EmployeeTIN,
    string EmployeeName,
    string CurrentEmployer,
    decimal GrossMonthlySalary,
    decimal AnnualTaxLiability,
    decimal TotalTaxPaid,
    decimal AdjustedMonthly,
    DateTime JoiningDate,
    bool HasPriorEmployer);
