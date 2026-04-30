using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Calculator;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly PayeTaxEasyDbContext _db;
    private readonly IIrdIntegrationService _ird;
    private readonly IAuditService _audit;
    private readonly INotificationService _notification;

    public PayrollService(
        PayeTaxEasyDbContext db,
        IIrdIntegrationService ird,
        IAuditService audit,
        INotificationService notification)
    {
        _db = db;
        _ird = ird;
        _audit = audit;
        _notification = notification;
    }

    public async Task<SalaryRecordDto> AddSalaryAsync(
        string employerTin, string employeeTin,
        decimal grossMonthlySalary, DateTime employmentStartDate, string financialYear)
    {
        var employer = await _db.Employers.FirstOrDefaultAsync(e => e.TIN == employerTin)
            ?? throw new KeyNotFoundException($"Employer TIN {employerTin} not found.");
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.TIN == employeeTin)
            ?? throw new KeyNotFoundException($"Employee TIN {employeeTin} not found.");

        // Fetch cumulative data from IRD
        var cumulative = await _ird.GetCumulativeDataAsync(employeeTin, financialYear, employer.Id);
        int remainingMonths = GetRemainingMonths(employmentStartDate);
        decimal projectedAnnual = cumulative.CumulativeIncome + (grossMonthlySalary * remainingMonths);
        decimal annualTax = PayeCalculator.CalculateAnnualTax(projectedAnnual);
        decimal monthlyDeduction = PayeCalculator.CalculateAdjustedDeduction(
            projectedAnnual, cumulative.CumulativeDeduction, remainingMonths);
        bool isOverpaid = monthlyDeduction == 0 && annualTax <= cumulative.CumulativeDeduction;

        var payroll = new EmployeePayroll
        {
            EmployerId = employer.Id,
            EmployeeId = employee.Id,
            GrossMonthlySalary = grossMonthlySalary,
            EmploymentStartDate = employmentStartDate,
            EffectiveDate = employmentStartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.EmployeePayrolls.Add(payroll);
        await _db.SaveChangesAsync();

        var deduction = new MonthlyDeduction
        {
            EmployeePayrollId = payroll.Id,
            Month = employmentStartDate.Month,
            Year = employmentStartDate.Year,
            GrossIncome = grossMonthlySalary,
            AnnualTaxLiability = annualTax,
            MonthlyDeductionAmount = monthlyDeduction,
            CumulativeDeductionAtCalculation = cumulative.CumulativeDeduction,
            RemainingMonthsAtCalculation = remainingMonths,
            CalculationTrigger = "InitialEntry",
            IsOverpaid = isOverpaid,
            CalculatedAt = DateTime.UtcNow
        };
        _db.MonthlyDeductions.Add(deduction);
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(employerTin, "Employer", "SalaryAdded",
            "EmployeePayroll", payroll.Id.ToString(), financialYear);

        return new SalaryRecordDto(payroll.Id, employeeTin, grossMonthlySalary,
            annualTax, monthlyDeduction, employmentStartDate, financialYear);
    }

    public async Task<SalaryRecordDto> UpdateSalaryAsync(
        Guid salaryRecordId, decimal newGrossMonthlySalary, DateTime effectiveDate)
    {
        var payroll = await _db.EmployeePayrolls
            .Include(p => p.Employee)
            .Include(p => p.Employer)
            .FirstOrDefaultAsync(p => p.Id == salaryRecordId)
            ?? throw new KeyNotFoundException($"Salary record {salaryRecordId} not found.");

        // Check if any deductions for this period are locked
        var locked = await _db.MonthlyDeductions
            .AnyAsync(d => d.EmployeePayrollId == salaryRecordId && d.IsLocked);
        if (locked)
            throw new InvalidOperationException("PAYROLL_004: Payroll period is locked.");

        payroll.GrossMonthlySalary = newGrossMonthlySalary;
        payroll.EffectiveDate = effectiveDate;
        await _db.SaveChangesAsync();

        // Recalculate from effective date
        string financialYear = GetFinancialYear(effectiveDate);
        var cumulative = await _ird.GetCumulativeDataAsync(
            payroll.Employee.TIN, financialYear, payroll.EmployerId);
        int remainingMonths = GetRemainingMonths(effectiveDate);
        decimal projectedAnnual = cumulative.CumulativeIncome + (newGrossMonthlySalary * remainingMonths);
        decimal annualTax = PayeCalculator.CalculateAnnualTax(projectedAnnual);
        decimal monthlyDeduction = PayeCalculator.CalculateAdjustedDeduction(
            projectedAnnual, cumulative.CumulativeDeduction, remainingMonths);

        _db.MonthlyDeductions.Add(new MonthlyDeduction
        {
            EmployeePayrollId = payroll.Id,
            Month = effectiveDate.Month,
            Year = effectiveDate.Year,
            GrossIncome = newGrossMonthlySalary,
            AnnualTaxLiability = annualTax,
            MonthlyDeductionAmount = monthlyDeduction,
            CumulativeDeductionAtCalculation = cumulative.CumulativeDeduction,
            RemainingMonthsAtCalculation = remainingMonths,
            CalculationTrigger = "SalaryAdjustment",
            CalculatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new SalaryRecordDto(payroll.Id, payroll.Employee.TIN,
            newGrossMonthlySalary, annualTax, monthlyDeduction, effectiveDate, financialYear);
    }

    public async Task<IEnumerable<SalaryRecordDto>> GetSalaryHistoryAsync(
        string employerTin, string employeeTin)
    {
        var records = await _db.EmployeePayrolls
            .Include(p => p.Employee)
            .Include(p => p.Employer)
            .Where(p => p.Employer.TIN == employerTin && p.Employee.TIN == employeeTin)
            .OrderByDescending(p => p.EffectiveDate)
            .ToListAsync();

        return records.Select(p => new SalaryRecordDto(
            p.Id, p.Employee.TIN, p.GrossMonthlySalary,
            PayeCalculator.CalculateAnnualTax(p.GrossMonthlySalary * 12),
            PayeCalculator.CalculateMonthlyDeduction(p.GrossMonthlySalary * 12),
            p.EffectiveDate,
            GetFinancialYear(p.EffectiveDate)));
    }

    public async Task<IEnumerable<DeductionSummaryDto>> GetDeductionSummaryAsync(
        string employerTin, string period)
    {
        var employer = await _db.Employers.FirstOrDefaultAsync(e => e.TIN == employerTin)
            ?? throw new KeyNotFoundException($"Employer TIN {employerTin} not found.");

        // Parse period to get the selected month/year
        if (!DateTime.TryParse($"{period}-01", out var periodDate))
            periodDate = DateTime.UtcNow;

        var payrolls = await _db.EmployeePayrolls
            .Include(p => p.Employee)
            .Include(p => p.MonthlyDeductions)
            .Where(p => p.EmployerId == employer.Id && p.IsActive)
            .ToListAsync();

        // Load IRD cache for all employees
        var irdCaches = await _db.IrdCumulativeCaches.ToListAsync();

        return payrolls.Select(p =>
        {
            var allDeductions = p.MonthlyDeductions
                .OrderBy(d => d.Year).ThenBy(d => d.Month).ToList();

            // Get IRD prior employer data
            var irdData = irdCaches
                .Where(c => c.EmployeeTIN == p.Employee.TIN)
                .OrderByDescending(c => c.RetrievedAt)
                .FirstOrDefault();
            decimal priorDeduction = irdData?.CumulativeDeduction ?? 0;

            // ═══════════════════════════════════════════════════════════════════
            // YOUR FORMULA:
            // Step 1: Standard monthly = AnnualTax(currentSalary × 12) / 12
            // Step 2: Tax for remaining months = Standard monthly × remaining months
            // Step 3: Adjusted monthly = (Tax for remaining months − cumulative paid) / remaining months
            // Step 4: Next FY = Standard monthly (no adjustments)
            // ═══════════════════════════════════════════════════════════════════

            // Standard monthly based on CURRENT salary (as if full year)
            decimal annualTaxOnCurrentSalary = PayeCalculator.CalculateAnnualTax(p.GrossMonthlySalary * 12);
            decimal standardMonthly = Math.Round(annualTaxOnCurrentSalary / 12, 0);

            // Remaining months in FY from joining date
            var fyEnd = new DateTime(2026, 3, 31);
            var joinDate = p.EmploymentStartDate;
            int remainingMonths = ((fyEnd.Year - joinDate.Year) * 12) + fyEnd.Month - joinDate.Month + 1;
            remainingMonths = Math.Max(1, Math.Min(12, remainingMonths));

            // Tax employer would charge for remaining months (without our system)
            decimal taxForRemainingMonths = standardMonthly * remainingMonths;

            // Adjusted monthly = (taxForRemainingMonths − already paid) / remaining months
            decimal adjustedMonthly = Math.Max(0, Math.Round((taxForRemainingMonths - priorDeduction) / remainingMonths, 0));

            // Total tax paid so far (prior + what we've charged)
            decimal totalPaid = priorDeduction + allDeductions.Sum(d => d.MonthlyDeductionAmount);

            // Annual tax liability = standard monthly × 12 (based on current salary)
            decimal annualTax = annualTaxOnCurrentSalary;

            // Remaining tax = what's left to collect this FY
            decimal remainingTax = Math.Max(0, taxForRemainingMonths - priorDeduction - allDeductions.Sum(d => d.MonthlyDeductionAmount));

            bool hasPrior = priorDeduction > 0;
            bool isOverpaid = priorDeduction >= taxForRemainingMonths;

            return new DeductionSummaryDto(
                p.Employee.TIN,
                p.Employee.FullName,
                p.GrossMonthlySalary,
                adjustedMonthly,
                totalPaid,
                hasPrior,
                isOverpaid,
                annualTax,
                remainingTax,
                remainingMonths,
                GetScenario(p, periodDate),
                p.EmploymentStartDate);
        });
    }

    private static string GetScenario(EmployeePayroll p, DateTime periodDate)
    {
        if (p.EmploymentEndDate.HasValue && p.EmploymentEndDate < periodDate)
            return "Resigned";
        if (p.EmploymentStartDate > new DateTime(p.EmploymentStartDate.Year, 4, 1))
            return "Mid-Year Joiner";
        var deductions = p.MonthlyDeductions.OrderBy(d => d.Year).ThenBy(d => d.Month).ToList();
        if (deductions.Count > 1)
        {
            var salaries = deductions.Select(d => d.GrossIncome).Distinct().ToList();
            if (salaries.Count > 1)
            {
                return deductions.First().GrossIncome < deductions.Last().GrossIncome
                    ? "Salary Increased"
                    : "Salary Decreased";
            }
        }
        return "Stable";
    }

    public async Task FinalizePeriodAsync(string employerTin, string period)
    {
        var employer = await _db.Employers.FirstOrDefaultAsync(e => e.TIN == employerTin)
            ?? throw new KeyNotFoundException($"Employer TIN {employerTin} not found.");

        if (!DateTime.TryParse($"{period}-01", out var periodDate))
            throw new ArgumentException($"Invalid period format: {period}. Expected YYYY-MM.");

        var deductions = await _db.MonthlyDeductions
            .Include(d => d.EmployeePayroll)
            .Where(d => d.EmployeePayroll.EmployerId == employer.Id
                && d.Year == periodDate.Year && d.Month == periodDate.Month)
            .ToListAsync();

        foreach (var d in deductions) d.IsLocked = true;
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(employerTin, "Employer", "PeriodFinalized",
            "PayrollPeriod", period);
    }

    public async Task<SubmissionResultDto> SubmitPayrollAsync(
        string employerTin, PayrollSubmissionRequestDto request)
    {
        var employer = await _db.Employers.FirstOrDefaultAsync(e => e.TIN == employerTin)
            ?? throw new KeyNotFoundException($"Employer TIN {employerTin} not found.");

        // Deadline check: 30 November
        var fyEnd = GetFinancialYearEnd(request.FinancialYear);
        var deadline = new DateTime(fyEnd.Year + 1, 11, 30);
        if (DateTime.UtcNow > deadline)
            throw new InvalidOperationException("PAYROLL_003: Filing deadline has passed.");

        var submission = new PayrollSubmission
        {
            EmployerId = employer.Id,
            FinancialYear = request.FinancialYear,
            Status = "Pending",
            TotalPAYEAmount = request.Lines.Sum(l => l.MonthlyDeduction),
            SubmittedAt = DateTime.UtcNow,
            SubmissionLines = request.Lines.Select(l => new SubmissionLine
            {
                EmployeeTIN = l.EmployeeTIN,
                GrossSalary = l.GrossSalary,
                MonthlyDeductionAmount = l.MonthlyDeduction,
                Month = l.Month,
                Year = l.Year
            }).ToList()
        };
        _db.PayrollSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        var ack = await _ird.SubmitToIrdAsync(submission.Id);

        await _audit.RecordAsync(employerTin, "Employer", "PayrollSubmitted",
            "PayrollSubmission", submission.Id.ToString(), request.FinancialYear,
            additionalData: $"IRDRef:{ack.ReferenceNumber},Total:{submission.TotalPAYEAmount}");

        await _notification.SendSubmissionConfirmedAsync(employer.ContactEmail,
            new SubmissionConfirmedPayload(ack.ReferenceNumber, ack.AcceptedAt, submission.TotalPAYEAmount));

        return new SubmissionResultDto(submission.Id, ack.ReferenceNumber,
            ack.Status, submission.TotalPAYEAmount, submission.SubmittedAt);
    }

    public async Task<SubmissionResultDto> SubmitBulkPayrollAsync(
        string employerTin, BulkPayrollSubmissionRequestDto request)
    {
        var singleRequest = new PayrollSubmissionRequestDto(
            request.FinancialYear,
            $"{DateTime.UtcNow:yyyy-MM}",
            request.Lines);
        return await SubmitPayrollAsync(employerTin, singleRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int GetRemainingMonths(DateTime fromDate)
    {
        // Financial year ends 31 March
        var fyEnd = fromDate.Month >= 4
            ? new DateTime(fromDate.Year + 1, 3, 31)
            : new DateTime(fromDate.Year, 3, 31);
        int months = ((fyEnd.Year - fromDate.Year) * 12) + fyEnd.Month - fromDate.Month + 1;
        return Math.Max(1, Math.Min(12, months));
    }

    private static string GetFinancialYear(DateTime date)
    {
        int startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }

    private static DateTime GetFinancialYearEnd(string financialYear)
    {
        // e.g. "2024-25" → 31 March 2025
        var parts = financialYear.Split('-');
        int endYear = int.Parse(parts[0]) + 1;
        return new DateTime(endYear, 3, 31);
    }
}
