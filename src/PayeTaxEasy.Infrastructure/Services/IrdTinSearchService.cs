using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Calculator;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayeTaxEasy.Infrastructure.Services;

public class IrdTinSearchService : IIrdTinSearchService
{
    private readonly PayeTaxEasyDbContext _db;

    public IrdTinSearchService(PayeTaxEasyDbContext db)
    {
        _db = db;
    }

    public async Task<EmployeeTinSearchResultDto?> SearchEmployeeByTinAsync(string tin, string financialYear)
    {
        // 1. Query Employees table by TIN; return null if not found
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.TIN == tin);
        if (employee == null) return null;

        // 2. Load active EmployeePayroll with Employer navigation property
        var activePayroll = await _db.EmployeePayrolls
            .Include(p => p.Employer)
            .Include(p => p.MonthlyDeductions)
            .Where(p => p.EmployeeId == employee.Id && p.IsActive)
            .OrderByDescending(p => p.EmploymentStartDate)
            .FirstOrDefaultAsync();

        if (activePayroll == null) return null;

        // 3. Load IrdCumulativeCache for prior employer data (same source as PayrollService)
        var irdCache = await _db.IrdCumulativeCaches
            .Where(c => c.EmployeeTIN == tin && c.FinancialYear == financialYear)
            .OrderByDescending(c => c.CumulativeDeduction)
            .ThenByDescending(c => c.RetrievedAt)
            .FirstOrDefaultAsync();

        decimal priorEmployerIncome = irdCache?.CumulativeIncome ?? 0;
        decimal priorEmployerDeduction = irdCache?.CumulativeDeduction ?? 0;

        // 4. Compute tax using the SAME formulas as PayrollService / TaxReportController
        decimal projectedAnnual = activePayroll.GrossMonthlySalary * 12;
        decimal annualTax = PayeCalculator.CalculateAnnualTax(projectedAnnual);
        decimal standardMonthly = Math.Round(annualTax / 12, 0);

        // Remaining months from joining date to FY end (clamped 1–12)
        var fyEnd = new DateTime(GetFYEndYear(financialYear), 3, 31);
        var joinDate = activePayroll.EmploymentStartDate;
        int remainingMonths = ((fyEnd.Year - joinDate.Year) * 12) + fyEnd.Month - joinDate.Month + 1;
        remainingMonths = Math.Max(1, Math.Min(12, remainingMonths));

        // WITHOUT system total = standardMonthly × remainingMonths
        decimal withoutSystemTotal = standardMonthly * remainingMonths;

        // WITH system total = Max(0, withoutSystemTotal − priorDeduction)
        decimal withSystemTotal = Math.Max(0, withoutSystemTotal - priorEmployerDeduction);

        // Adjusted monthly = Max(0, Round(withSystemTotal / remainingMonths, 0))
        decimal adjustedMonthly = Math.Max(0, Math.Round(withSystemTotal / remainingMonths, 0));

        // Savings per month
        decimal savingsPerMonth = standardMonthly - adjustedMonthly;

        // 5. Load MonthlyDeductions across all employers via EmployeePayrolls join
        //    (same join path as EmployeePortalService.GetDeductionHistoryAsync)
        var allEmployerDeductions = await _db.MonthlyDeductions
            .Include(d => d.EmployeePayroll)
                .ThenInclude(p => p.Employer)
            .Where(d => d.EmployeePayroll.Employee.TIN == tin)
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToListAsync();

        // Current employer YTD from active payroll deductions
        decimal currentEmployerYTD = activePayroll.MonthlyDeductions
            .Sum(d => d.MonthlyDeductionAmount);

        // Total tax paid = prior employer deduction + all deductions across all employers
        decimal totalTaxPaid = allEmployerDeductions.Sum(d => d.MonthlyDeductionAmount);

        // Remaining tax for year
        decimal remainingTax = Math.Max(0, withSystemTotal);

        // 6. Build monthly history entries
        var monthlyHistory = allEmployerDeductions.Select(d => new MonthlyHistoryEntryDto(
            $"{d.Year}-{d.Month:D2}",
            new DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy"),
            d.EmployeePayroll.Employer.OrganizationName,
            d.GrossIncome,
            d.MonthlyDeductionAmount,
            d.CumulativeDeductionAtCalculation + d.MonthlyDeductionAmount
        )).ToList();

        // 7. Build tax slab breakdown using PayeCalculator slab rates
        var slabs = GetSlabBreakdown(projectedAnnual);

        // 8. Return fully populated EmployeeTinSearchResultDto
        return new EmployeeTinSearchResultDto(
            EmployeeTIN: tin,
            EmployeeName: employee.FullName,
            CurrentEmployer: activePayroll.Employer.OrganizationName,
            GrossMonthlySalary: activePayroll.GrossMonthlySalary,
            ProjectedAnnualIncome: projectedAnnual,
            TaxRelief: 1_800_000m,
            AnnualTaxLiability: annualTax,
            StandardMonthly: standardMonthly,
            AdjustedMonthly: adjustedMonthly,
            PriorEmployerIncome: priorEmployerIncome,
            PriorEmployerDeduction: priorEmployerDeduction,
            CurrentEmployerYTD: currentEmployerYTD,
            TotalTaxPaid: totalTaxPaid,
            RemainingTaxForYear: remainingTax,
            RemainingMonthsInFY: remainingMonths,
            WithoutSystemTotal: withoutSystemTotal,
            WithSystemTotal: withSystemTotal,
            SavingsPerMonth: savingsPerMonth,
            JoiningDate: activePayroll.EmploymentStartDate,
            HasPriorEmployer: priorEmployerDeduction > 0,
            FinancialYear: financialYear,
            Slabs: slabs,
            MonthlyHistory: monthlyHistory
        );
    }

    public async Task<byte[]> GenerateEmployeePdfAsync(string tin, string financialYear)
    {
        var data = await SearchEmployeeByTinAsync(tin, financialYear);
        if (data == null)
            throw new KeyNotFoundException($"Employee TIN {tin} not found.");

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // ── Header ───────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PAYE Tax Easy").Bold().FontSize(18).FontColor("#003366");
                            c.Item().Text("IRD Officer Report").FontSize(13).FontColor("#555555");
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Financial Year: {financialYear}").FontSize(9);
                            c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm}").FontSize(9).FontColor("#888888");
                        });
                    });
                    col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#003366");
                });

                // ── Content ──────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    // Employee Info Section
                    col.Item().Background("#f0f4f8").Padding(10).Column(info =>
                    {
                        info.Item().Text("Employee Information").Bold().FontSize(11).FontColor("#003366");
                        info.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Name: {data.EmployeeName}").Bold();
                                c.Item().Text($"TIN: {data.EmployeeTIN}");
                                c.Item().Text($"Employer: {data.CurrentEmployer}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Gross Monthly Salary: Rs. {data.GrossMonthlySalary:N0}").Bold();
                                c.Item().Text($"Joining Date: {data.JoiningDate:dd MMM yyyy}");
                            });
                        });
                    });

                    // Tax Calculation Summary Table
                    col.Item().PaddingTop(10).Text("Tax Calculation Summary").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });

                        void SummaryRow(string label, string value, bool bold = false, string? color = null)
                        {
                            var labelCell = table.Cell().Padding(6).Text(label);
                            if (bold) labelCell.Bold();
                            var valueCell = table.Cell().Padding(6).AlignRight().Text(value).FontColor(color ?? "#000000");
                            if (bold) valueCell.Bold();
                        }

                        SummaryRow("Projected Annual Income", $"Rs. {data.ProjectedAnnualIncome:N0}", true, "#003366");
                        SummaryRow("Tax Relief", $"Rs. {data.TaxRelief:N0}");
                        SummaryRow("Annual Tax Liability", $"Rs. {data.AnnualTaxLiability:N0}", true, "#003366");
                        SummaryRow("Standard Monthly Deduction", $"Rs. {data.StandardMonthly:N0}");
                        SummaryRow("Adjusted Monthly Deduction", $"Rs. {data.AdjustedMonthly:N0}", true, "#17a2b8");
                    });

                    // Tax Slab Breakdown Table
                    col.Item().PaddingTop(10).Text("Tax Slab Breakdown").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(6).Text("Income Band").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Rate").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Taxable Amount").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Tax").FontColor("#FFFFFF").Bold();
                        });
                        foreach (var slab in data.Slabs)
                        {
                            table.Cell().Padding(5).Text(slab.Band);
                            table.Cell().Padding(5).Text(slab.Rate);
                            table.Cell().Padding(5).AlignRight().Text($"Rs. {slab.TaxableAmount:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"Rs. {slab.Tax:N0}");
                        }
                    });

                    // WITHOUT vs WITH Comparison Notice (amber box)
                    col.Item().PaddingTop(12).Border(1.5f).BorderColor("#f59e0b")
                        .Background("#fffbea").Padding(12).Column(notice =>
                    {
                        notice.Item().Text("⚠  Important Notice — PAYE Tax Adjustment by PAYE Tax Easy")
                            .Bold().FontSize(11).FontColor("#92400e");

                        // WITHOUT system
                        notice.Item().PaddingTop(8).Background("#ffffff").Border(1).BorderColor("#e74c3c").Padding(10).Column(c =>
                        {
                            c.Item().Text("❌ WITHOUT PAYE Tax Easy (Current System)").Bold().FontSize(9).FontColor("#e74c3c");
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#555555"));
                                t.Span($"Standard monthly deduction: Rs. {data.StandardMonthly:N0} × {data.RemainingMonthsInFY} months = ");
                                t.Span($"Rs. {data.WithoutSystemTotal:N0}").Bold().FontColor("#e74c3c");
                                t.Span(" (ignores cumulative tax already paid)");
                            });
                        });

                        // WITH system
                        notice.Item().PaddingTop(6).Background("#ffffff").Border(1).BorderColor("#27ae60").Padding(10).Column(c =>
                        {
                            c.Item().Text("✅ WITH PAYE Tax Easy (Our Solution)").Bold().FontSize(9).FontColor("#27ae60");
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#555555"));
                                t.Span($"Rs. {data.WithoutSystemTotal:N0} − Rs. {data.PriorEmployerDeduction:N0} (already paid) = ");
                                t.Span($"Rs. {data.WithSystemTotal:N0}").Bold().FontColor("#003366");
                                t.Span($" ÷ {data.RemainingMonthsInFY} months = ");
                                t.Span($"Rs. {data.AdjustedMonthly:N0}/month").Bold().FontColor("#17a2b8");
                            });
                        });

                        if (data.SavingsPerMonth > 0)
                        {
                            notice.Item().PaddingTop(6).Background("#d4edda").Padding(8).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#155724"));
                                t.Span("💰 Employee saves ").Bold();
                                t.Span($"Rs. {data.SavingsPerMonth:N0} per month").Bold().FontColor("#003366");
                                t.Span($" (Rs. {data.SavingsPerMonth * data.RemainingMonthsInFY:N0} total for remaining {data.RemainingMonthsInFY} months)");
                            });
                        }

                        notice.Item().PaddingTop(8).Background("#fef3c7").Padding(8).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(9).FontColor("#78350f"));
                            t.Span("📌  Note: ").Bold().FontColor("#92400e");
                            t.Span("The adjusted amount of ");
                            t.Span($"Rs. {data.AdjustedMonthly:N0}").Bold().FontColor("#17a2b8");
                            t.Span($" will be charged for the remaining ");
                            t.Span($"{data.RemainingMonthsInFY} month{(data.RemainingMonthsInFY != 1 ? "s" : "")}").Bold().FontColor("#003366");
                            t.Span($" of the current financial year ({financialYear}). ");
                            t.Span("From the next financial year, ");
                            t.Span("no adjustments will be applied").Bold();
                            t.Span(" — the standard monthly deduction of ");
                            t.Span($"Rs. {data.StandardMonthly:N0}").Bold().FontColor("#003366");
                            t.Span(" will be charged fresh based on the employee's current salary.");
                        });
                    });

                    // Monthly Deduction History Table
                    col.Item().PaddingTop(10).Text("Monthly Deduction History").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(6).Text("Month").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Employer").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Gross Income").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Deduction").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Cumulative").FontColor("#FFFFFF").Bold();
                        });
                        bool alt = false;
                        foreach (var entry in data.MonthlyHistory)
                        {
                            var bg = alt ? "#f8f9fa" : "#ffffff";
                            table.Cell().Background(bg).Padding(5).Text(entry.MonthLabel);
                            table.Cell().Background(bg).Padding(5).Text(entry.EmployerName);
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {entry.GrossIncome:N0}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {entry.DeductionAmount:N0}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {entry.CumulativeAtMonth:N0}");
                            alt = !alt;
                        }
                    });
                });

                // ── Footer ───────────────────────────────────────────────
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("PAYE Tax Easy | Inland Revenue Department Sri Lanka | ").FontSize(8).FontColor("#888888");
                    t.Span("Inland Revenue (Amendment) Act No. 02 of 2025").FontSize(8).FontColor("#888888");
                });
            });
        });

        return pdf.GeneratePdf();
    }

    public async Task<EmployerTinSearchResultDto?> SearchEmployerByTinAsync(string tin, string financialYear)
    {
        // 1. Query Employers table by TIN; return null if not found
        var employer = await _db.Employers.FirstOrDefaultAsync(e => e.TIN == tin);
        if (employer == null) return null;

        // 2. Load EmployeePayrolls with Employee navigation (employee roster)
        //    Same filter as IrdDashboardService.GetAllEmployersWithSubmissionsAsync: EmployerId match + IsActive
        var payrolls = await _db.EmployeePayrolls
            .Include(p => p.Employee)
            .Where(p => p.EmployerId == employer.Id)
            .ToListAsync();

        // 3. Load PayrollSubmissions filtered by employer ID and financial year
        //    Same query path as IrdDashboardService.GetAllEmployersWithSubmissionsAsync:
        //    FinancialYear == financialYear && Status == "Accepted"
        var submissions = await _db.PayrollSubmissions
            .Where(s => s.EmployerId == employer.Id && s.FinancialYear == financialYear && s.Status == "Accepted")
            .ToListAsync();

        // 4. Compute totals: employee count (active only), submission count, total PAYE submitted
        var activePayrolls = payrolls.Where(p => p.IsActive).ToList();
        int employeeCount = activePayrolls.Count;
        int totalSubmissions = submissions.Count;
        decimal totalPAYESubmitted = submissions.Sum(s => s.TotalPAYEAmount);

        var latest = submissions.OrderByDescending(s => s.SubmittedAt).FirstOrDefault();

        // 5. Build employee roster DTOs
        var employees = payrolls.Select(p => new EmployerEmployeeDto(
            p.Employee.TIN,
            p.Employee.FullName,
            p.GrossMonthlySalary,
            p.EmploymentStartDate,
            p.IsActive
        )).ToList();

        // 6. Build submission DTOs
        var submissionDtos = submissions.Select(s => new EmployerSubmissionDto(
            s.IRDReferenceNumber,
            s.Status,
            s.TotalPAYEAmount,
            s.SubmittedAt,
            s.FinancialYear
        )).ToList();

        // 7. Return fully populated EmployerTinSearchResultDto
        return new EmployerTinSearchResultDto(
            EmployerTIN: tin,
            OrganizationName: employer.OrganizationName,
            RegistrationNumber: employer.RegistrationNumber,
            ContactEmail: employer.ContactEmail,
            EmployeeCount: employeeCount,
            TotalSubmissions: totalSubmissions,
            TotalPAYESubmitted: totalPAYESubmitted,
            LatestSubmissionDate: latest?.SubmittedAt,
            LatestSubmissionRef: latest?.IRDReferenceNumber ?? "—",
            FinancialYear: financialYear,
            Employees: employees,
            Submissions: submissionDtos
        );
    }

    public async Task<byte[]> GenerateEmployerPdfAsync(string tin, string financialYear)
    {
        var data = await SearchEmployerByTinAsync(tin, financialYear);
        if (data == null)
            throw new KeyNotFoundException($"Employer TIN {tin} not found.");

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // ── Header ───────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PAYE Tax Easy").Bold().FontSize(18).FontColor("#003366");
                            c.Item().Text("IRD Officer Report").FontSize(13).FontColor("#555555");
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Financial Year: {financialYear}").FontSize(9);
                            c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm}").FontSize(9).FontColor("#888888");
                        });
                    });
                    col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#003366");
                });

                // ── Content ──────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    // Employer Info Section
                    col.Item().Background("#f0f4f8").Padding(10).Column(info =>
                    {
                        info.Item().Text("Employer Information").Bold().FontSize(11).FontColor("#003366");
                        info.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Organization: {data.OrganizationName}").Bold();
                                c.Item().Text($"TIN: {data.EmployerTIN}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Registration: {data.RegistrationNumber}").Bold();
                                c.Item().Text($"Email: {data.ContactEmail}");
                            });
                        });
                    });

                    // Employee Roster Table
                    col.Item().PaddingTop(10).Text("Employee Roster").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(6).Text("TIN").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Name").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Salary").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Joining Date").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Active").FontColor("#FFFFFF").Bold();
                        });
                        bool alt = false;
                        foreach (var emp in data.Employees)
                        {
                            var bg = alt ? "#f8f9fa" : "#ffffff";
                            table.Cell().Background(bg).Padding(5).Text(emp.EmployeeTIN);
                            table.Cell().Background(bg).Padding(5).Text(emp.EmployeeName);
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {emp.GrossMonthlySalary:N0}");
                            table.Cell().Background(bg).Padding(5).Text(emp.JoiningDate.ToString("dd MMM yyyy"));
                            table.Cell().Background(bg).Padding(5).Text(emp.IsActive ? "Yes" : "No")
                                .FontColor(emp.IsActive ? "#27ae60" : "#e67e22");
                            alt = !alt;
                        }
                    });

                    // Submission History Table
                    col.Item().PaddingTop(10).Text("Submission History").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(6).Text("Reference").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Status").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Amount").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Date").FontColor("#FFFFFF").Bold();
                        });
                        bool alt = false;
                        foreach (var sub in data.Submissions)
                        {
                            var bg = alt ? "#f8f9fa" : "#ffffff";
                            table.Cell().Background(bg).Padding(5).Text(sub.IRDReferenceNumber ?? "—");
                            table.Cell().Background(bg).Padding(5).Text(sub.Status)
                                .FontColor(sub.Status == "Accepted" ? "#27ae60" : "#e67e22");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {sub.TotalPAYEAmount:N0}");
                            table.Cell().Background(bg).Padding(5).Text(sub.SubmittedAt.ToString("dd MMM yyyy"));
                            alt = !alt;
                        }
                    });

                    // Summary Totals
                    col.Item().PaddingTop(10).Background("#f0f4f8").Padding(10).Column(summary =>
                    {
                        summary.Item().Text("Summary").Bold().FontSize(11).FontColor("#003366");
                        summary.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Total Employees: {data.EmployeeCount}").Bold();
                                c.Item().Text($"Total Submissions: {data.TotalSubmissions}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Total PAYE Submitted: Rs. {data.TotalPAYESubmitted:N0}").Bold().FontColor("#27ae60");
                                c.Item().Text($"Latest Submission: {(data.LatestSubmissionDate.HasValue ? data.LatestSubmissionDate.Value.ToString("dd MMM yyyy") : "—")}");
                            });
                        });
                    });
                });

                // ── Footer ───────────────────────────────────────────────
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("PAYE Tax Easy | Inland Revenue Department Sri Lanka | ").FontSize(8).FontColor("#888888");
                    t.Span("Inland Revenue (Amendment) Act No. 02 of 2025").FontSize(8).FontColor("#888888");
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int GetFYEndYear(string financialYear)
    {
        // e.g. "2025-26" → 2026
        return int.Parse(financialYear.Split('-')[0]) + 1;
    }

    private static List<TaxSlabDto> GetSlabBreakdown(decimal annualIncome)
    {
        var slabs = new List<TaxSlabDto>();
        decimal taxable = Math.Max(0, annualIncome - 1_800_000m);

        slabs.Add(new TaxSlabDto("Up to Rs. 1,800,000", "0%", Math.Min(annualIncome, 1_800_000m), 0));

        decimal s1 = Math.Min(taxable, 1_000_000m);
        slabs.Add(new TaxSlabDto("Rs. 1,800,001 – Rs. 2,800,000", "6%", s1, s1 * 0.06m));
        taxable -= s1; if (taxable <= 0) return slabs;

        decimal s2 = Math.Min(taxable, 500_000m);
        slabs.Add(new TaxSlabDto("Rs. 2,800,001 – Rs. 3,300,000", "18%", s2, s2 * 0.18m));
        taxable -= s2; if (taxable <= 0) return slabs;

        decimal s3 = Math.Min(taxable, 500_000m);
        slabs.Add(new TaxSlabDto("Rs. 3,300,001 – Rs. 3,800,000", "24%", s3, s3 * 0.24m));
        taxable -= s3; if (taxable <= 0) return slabs;

        decimal s4 = Math.Min(taxable, 500_000m);
        slabs.Add(new TaxSlabDto("Rs. 3,800,001 – Rs. 4,300,000", "30%", s4, s4 * 0.30m));
        taxable -= s4; if (taxable <= 0) return slabs;

        slabs.Add(new TaxSlabDto("Above Rs. 4,300,000", "36%", taxable, taxable * 0.36m));
        return slabs;
    }
}
