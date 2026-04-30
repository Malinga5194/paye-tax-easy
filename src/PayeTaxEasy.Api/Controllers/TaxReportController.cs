using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Calculator;
using PayeTaxEasy.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("tax-report")]
[Authorize(Roles = "Employer,SystemAdmin")]
public class TaxReportController : ControllerBase
{
    private readonly PayeTaxEasyDbContext _db;

    public TaxReportController(PayeTaxEasyDbContext db) => _db = db;

    /// <summary>GET /tax-report/{tin}/{financialYear}/{period} — Get tax report data for popup</summary>
    [HttpGet("{tin}/{financialYear}/{period}")]
    public async Task<IActionResult> GetReport(string tin, string financialYear, string period)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.TIN == tin);
        if (employee == null) return NotFound(new { message = "Employee not found." });

        var payroll = await _db.EmployeePayrolls
            .Include(p => p.MonthlyDeductions)
            .Include(p => p.Employer)
            .FirstOrDefaultAsync(p => p.Employee.TIN == tin);

        if (payroll == null) return NotFound(new { message = "Payroll record not found." });

        if (!DateTime.TryParse($"{period}-01", out var periodDate))
            periodDate = DateTime.UtcNow;

        // Get IRD cumulative data
        var irdCache = await _db.IrdCumulativeCaches
            .Where(c => c.EmployeeTIN == tin && c.FinancialYear == financialYear)
            .OrderByDescending(c => c.RetrievedAt)
            .FirstOrDefaultAsync();

        decimal priorEmployerIncome = irdCache?.CumulativeIncome ?? 0;
        decimal priorEmployerDeduction = irdCache?.CumulativeDeduction ?? 0;

        var allDeductions = payroll.MonthlyDeductions
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToList();

        // ALL deductions across ALL employers for this employee (for history display)
        var allEmployerDeductions = await _db.MonthlyDeductions
            .Include(d => d.EmployeePayroll)
                .ThenInclude(p => p.Employer)
            .Where(d => d.EmployeePayroll.Employee.TIN == tin)
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToListAsync();

        // ═══════════════════════════════════════════════════════════════════
        // YOUR FORMULA (Chaminda example):
        // 1. Current salary Rs. 400,000 → annual = Rs. 4,800,000
        // 2. Annual tax on Rs. 4,800,000 = Rs. 600,000
        // 3. Standard monthly = Rs. 600,000 / 12 = Rs. 50,000
        // 4. WITHOUT system: Rs. 50,000 × 8 remaining = Rs. 400,000
        // 5. WITH system: Rs. 400,000 - Rs. 74,000 (IRD paid) = Rs. 326,000
        // 6. Adjusted monthly = Rs. 326,000 / 8 = Rs. 40,750
        // 7. Next FY: Rs. 50,000 (no adjustments)
        // ═══════════════════════════════════════════════════════════════════

        decimal annualTaxOnCurrentSalary = PayeCalculator.CalculateAnnualTax(payroll.GrossMonthlySalary * 12);
        decimal standardMonthly = Math.Round(annualTaxOnCurrentSalary / 12, 0);

        // Remaining months from joining date to end of FY
        var fyEnd = new DateTime(2026, 3, 31);
        var joinDate = payroll.EmploymentStartDate;
        int remainingMonths = ((fyEnd.Year - joinDate.Year) * 12) + fyEnd.Month - joinDate.Month + 1;
        remainingMonths = Math.Max(1, Math.Min(12, remainingMonths));

        // WITHOUT our system (current broken system)
        decimal withoutSystemTotal = standardMonthly * remainingMonths;
        decimal withoutSystemMonthly = standardMonthly;

        // WITH our system (adjusted using cumulative from IRD)
        decimal withSystemTotal = Math.Max(0, withoutSystemTotal - priorEmployerDeduction);
        decimal adjustedMonthly = Math.Max(0, Math.Round(withSystemTotal / remainingMonths, 0));

        // Savings for the employee
        decimal savingsPerMonth = withoutSystemMonthly - adjustedMonthly;
        decimal totalSavings = savingsPerMonth * remainingMonths;

        decimal currentEmployerYTD = allDeductions.Sum(d => d.MonthlyDeductionAmount);
        decimal totalYTD = priorEmployerDeduction + currentEmployerYTD;
        decimal annualTaxLiability = annualTaxOnCurrentSalary;
        // Remaining tax = what this employer needs to collect for this FY (the adjusted total)
        decimal remainingTax = withSystemTotal;
        decimal projectedAnnual = payroll.GrossMonthlySalary * 12;

        var report = new
        {
            // Employee info
            employeeTIN = tin,
            employeeName = employee.FullName,
            employerName = payroll.Employer.OrganizationName,
            joiningDate = payroll.EmploymentStartDate,
            financialYear,
            reportPeriod = period,
            generatedAt = DateTime.UtcNow,

            // Salary info
            grossMonthlySalary = payroll.GrossMonthlySalary,
            projectedAnnualIncome = projectedAnnual,

            // Tax calculation
            annualTaxLiability,
            standardMonthly,
            taxRelief = 1_800_000m,
            taxableIncome = Math.Max(0, projectedAnnual - 1_800_000m),

            // Comparison: WITHOUT system vs WITH system
            withoutSystemMonthly,
            withoutSystemTotal,
            withSystemTotal,
            savingsPerMonth,
            totalSavings,

            // Tax slab breakdown
            slabs = GetSlabBreakdown(projectedAnnual),

            // Prior employer (IRD data)
            priorEmployerIncome,
            priorEmployerDeduction,

            // Current employer
            currentEmployerYTD,
            totalTaxPaidToDate = totalYTD,

            // Remaining
            remainingTaxForYear = remainingTax,
            remainingMonthsInFY = remainingMonths,
            adjustedMonthlyDeduction = adjustedMonthly,

            // Monthly history (ALL employers)
            monthlyHistory = allEmployerDeductions.Select(d => new
            {
                month = $"{d.Year}-{d.Month:D2}",
                monthLabel = new DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy"),
                employerName = d.EmployeePayroll.Employer.OrganizationName,
                grossIncome = d.GrossIncome,
                deductionAmount = d.MonthlyDeductionAmount,
                cumulativeAtMonth = d.CumulativeDeductionAtCalculation + d.MonthlyDeductionAmount,
                trigger = d.CalculationTrigger
            }).ToList()
        };

        return Ok(report);
    }

    /// <summary>GET /tax-report/{tin}/{financialYear}/{period}/pdf — Download PDF report</summary>
    [HttpGet("{tin}/{financialYear}/{period}/pdf")]
    public async Task<IActionResult> GetPdf(string tin, string financialYear, string period)
    {
        // Reuse the report data
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.TIN == tin);
        if (employee == null) return NotFound();

        var payroll = await _db.EmployeePayrolls
            .Include(p => p.MonthlyDeductions)
            .Include(p => p.Employer)
            .FirstOrDefaultAsync(p => p.Employee.TIN == tin);
        if (payroll == null) return NotFound();

        if (!DateTime.TryParse($"{period}-01", out var periodDate))
            periodDate = DateTime.UtcNow;

        var irdCache = await _db.IrdCumulativeCaches
            .Where(c => c.EmployeeTIN == tin && c.FinancialYear == financialYear)
            .OrderByDescending(c => c.RetrievedAt)
            .FirstOrDefaultAsync();

        decimal priorDeduction = irdCache?.CumulativeDeduction ?? 0;
        decimal priorIncome = irdCache?.CumulativeIncome ?? 0;

        var allDeductions = payroll.MonthlyDeductions
            .OrderBy(d => d.Year).ThenBy(d => d.Month).ToList();

        // Same formula as report endpoint
        decimal annualTax = PayeCalculator.CalculateAnnualTax(payroll.GrossMonthlySalary * 12);
        decimal standardMonthly = Math.Round(annualTax / 12, 0);
        var fyEnd = new DateTime(2026, 3, 31);
        var joinDate = payroll.EmploymentStartDate;
        int remainingMonths = Math.Max(1, Math.Min(12,
            ((fyEnd.Year - joinDate.Year) * 12) + fyEnd.Month - joinDate.Month + 1));

        // WITHOUT system vs WITH system
        decimal withoutSystemTotal = standardMonthly * remainingMonths;
        decimal withSystemTotal = Math.Max(0, withoutSystemTotal - priorDeduction);
        decimal adjustedMonthly = Math.Max(0, Math.Round(withSystemTotal / remainingMonths, 0));
        decimal savingsPerMonth = standardMonthly - adjustedMonthly;

        decimal currentYTD = allDeductions.Sum(d => d.MonthlyDeductionAmount);
        decimal totalYTD = priorDeduction + currentYTD;
        decimal remainingTax = Math.Max(0, withSystemTotal - currentYTD);
        decimal projectedAnnual = payroll.GrossMonthlySalary * 12;

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PAYE Tax Easy").Bold().FontSize(18).FontColor("#003366");
                            c.Item().Text("PAYE Tax Deduction Report").FontSize(13).FontColor("#555555");
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Financial Year: {financialYear}").FontSize(9);
                            c.Item().Text($"As of: {periodDate:MMMM yyyy}").FontSize(9);
                            c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm}").FontSize(9).FontColor("#888888");
                        });
                    });
                    col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#003366");
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    // Employee Info
                    col.Item().Background("#f0f4f8").Padding(10).Column(info =>
                    {
                        info.Item().Text("Employee Information").Bold().FontSize(11).FontColor("#003366");
                        info.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Name: {employee.FullName}").Bold();
                                c.Item().Text($"TIN: {tin}");
                                c.Item().Text($"Employer: {payroll.Employer.OrganizationName}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Gross Monthly Salary: Rs. {payroll.GrossMonthlySalary:N0}").Bold();
                                c.Item().Text($"Projected Annual Income: Rs. {projectedAnnual:N0}");
                                c.Item().Text($"Tax Relief: Rs. 1,800,000");
                            });
                        });
                    });

                    col.Item().PaddingTop(10).Text("Tax Calculation Summary").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });
                        void Row(string label, string value, bool bold = false, string? color = null)
                        {
                            var labelCell = table.Cell().Padding(6).Text(label);
                            if (bold) labelCell.Bold();
                            var valueCell = table.Cell().Padding(6).AlignRight().Text(value).FontColor(color ?? "#000000");
                            if (bold) valueCell.Bold();
                        }
                        Row("Annual Tax Liability", $"Rs. {annualTax:N0}", true, "#003366");
                        Row("Standard Monthly (without adjustment)", $"Rs. {standardMonthly:N0}");
                        Row("Remaining Months in FY", remainingMonths.ToString());
                        Row("", "", false, null);
                        Row("WITHOUT PAYE Tax Easy:", $"Rs. {standardMonthly:N0} × {remainingMonths} = Rs. {withoutSystemTotal:N0}", false, "#e74c3c");
                        Row("Less: Cumulative Tax Already Paid (IRD)", $"(Rs. {priorDeduction:N0})", false, "#27ae60");
                        Row("WITH PAYE Tax Easy:", $"Rs. {withSystemTotal:N0}", true, "#003366");
                        Row("Adjusted Monthly Deduction (This FY)", $"Rs. {adjustedMonthly:N0}", true, "#17a2b8");
                        Row("Savings per Month", $"Rs. {savingsPerMonth:N0}", false, "#27ae60");
                        Row("", "", false, null);
                        Row("Tax Paid (Current Employer YTD)", $"Rs. {currentYTD:N0}");
                        Row("Total Tax Paid to Date", $"Rs. {totalYTD:N0}", true, "#27ae60");
                        Row("Remaining Tax for FY", $"Rs. {remainingTax:N0}", true, "#e67e22");
                        Row("", "", false, null);
                        Row("Next FY Monthly (No Adjustments)", $"Rs. {standardMonthly:N0}", true, "#003366");
                    });

                    // Tax Slab Breakdown
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
                        foreach (var slab in GetSlabBreakdown(projectedAnnual))
                        {
                            table.Cell().Padding(5).Text(slab.Band);
                            table.Cell().Padding(5).Text(slab.Rate);
                            table.Cell().Padding(5).AlignRight().Text($"Rs. {slab.TaxableAmount:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"Rs. {slab.Tax:N0}");
                        }
                    });

                    // ── Important Notice ──────────────────────────────────────────────
                    col.Item().PaddingTop(12).Border(1.5f).BorderColor("#f59e0b")
                        .Background("#fffbea").Padding(12).Column(notice =>
                    {
                        notice.Item().Text("⚠  Important Notice — PAYE Tax Adjustment by PAYE Tax Easy")
                            .Bold().FontSize(11).FontColor("#92400e");

                        // Comparison: WITHOUT vs WITH
                        notice.Item().PaddingTop(8).Background("#ffffff").Border(1).BorderColor("#e74c3c").Padding(10).Column(c =>
                        {
                            c.Item().Text("❌ WITHOUT PAYE Tax Easy (Current System)").Bold().FontSize(9).FontColor("#e74c3c");
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#555555"));
                                t.Span($"Standard monthly deduction: Rs. {standardMonthly:N0} × {remainingMonths} months = ");
                                t.Span($"Rs. {withoutSystemTotal:N0}").Bold().FontColor("#e74c3c");
                                t.Span(" (ignores cumulative tax already paid)");
                            });
                        });

                        notice.Item().PaddingTop(6).Background("#ffffff").Border(1).BorderColor("#27ae60").Padding(10).Column(c =>
                        {
                            c.Item().Text("✅ WITH PAYE Tax Easy (Our Solution)").Bold().FontSize(9).FontColor("#27ae60");
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#555555"));
                                t.Span($"Rs. {withoutSystemTotal:N0} − Rs. {priorDeduction:N0} (already paid) = ");
                                t.Span($"Rs. {withSystemTotal:N0}").Bold().FontColor("#003366");
                                t.Span($" ÷ {remainingMonths} months = ");
                                t.Span($"Rs. {adjustedMonthly:N0}/month").Bold().FontColor("#17a2b8");
                            });
                        });

                        if (savingsPerMonth > 0)
                        {
                            notice.Item().PaddingTop(6).Background("#d4edda").Padding(8).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#155724"));
                                t.Span("💰 Employee saves ").Bold();
                                t.Span($"Rs. {savingsPerMonth:N0} per month").Bold().FontColor("#003366");
                                t.Span($" (Rs. {savingsPerMonth * remainingMonths:N0} total for remaining {remainingMonths} months)");
                            });
                        }

                        notice.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Cumulative Tax Already Paid").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {priorDeduction:N0}").Bold().FontSize(12).FontColor("#27ae60");
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Adjusted Monthly (This FY)").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {adjustedMonthly:N0}").Bold().FontSize(12).FontColor("#17a2b8");
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Next FY Monthly (No Adjustments)").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {standardMonthly:N0}").Bold().FontSize(12).FontColor("#003366");
                            });
                        });

                        notice.Item().PaddingTop(8).Background("#fef3c7").Padding(8).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(9).FontColor("#78350f"));
                            t.Span("📌  Note: ").Bold().FontColor("#92400e");
                            t.Span("The adjusted amount of ");
                            t.Span($"Rs. {adjustedMonthly:N0}").Bold().FontColor("#17a2b8");
                            t.Span($" will be charged for the remaining ");
                            t.Span($"{remainingMonths} month{(remainingMonths != 1 ? "s" : "")}").Bold().FontColor("#003366");
                            t.Span($" of the current financial year ({financialYear}). ");
                            t.Span("From the next financial year, ");
                            t.Span("no adjustments will be applied").Bold();
                            t.Span(" — the standard monthly deduction of ");
                            t.Span($"Rs. {standardMonthly:N0}").Bold().FontColor("#003366");
                            t.Span(" will be charged fresh based on the employee's current salary.");
                        });
                    });

                    // Monthly History
                    col.Item().PaddingTop(10).Text("Monthly Deduction History").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(6).Text("Month").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Gross Salary").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Monthly Deduction").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#003366").Padding(6).Text("Cumulative Total").FontColor("#FFFFFF").Bold();
                        });
                        decimal cumulative = priorDeduction;
                        bool alt = false;
                        foreach (var d in payroll.MonthlyDeductions.OrderBy(x => x.Year).ThenBy(x => x.Month))
                        {
                            cumulative += d.MonthlyDeductionAmount;
                            var bg = alt ? "#f8f9fa" : "#ffffff";
                            table.Cell().Background(bg).Padding(5).Text(new DateTime(d.Year, d.Month, 1).ToString("MMM yyyy"));
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {d.GrossIncome:N0}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {d.MonthlyDeductionAmount:N0}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {cumulative:N0}");
                            alt = !alt;
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("PAYE Tax Easy | Inland Revenue Department Sri Lanka | ").FontSize(8).FontColor("#888888");
                    t.Span($"Inland Revenue (Amendment) Act No. 02 of 2025").FontSize(8).FontColor("#888888");
                });
            });
        });

        var pdfBytes = pdf.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"PAYE_Report_{tin}_{period}.pdf");
    }

    private static List<SlabRow> GetSlabBreakdown(decimal annualIncome)
    {
        var slabs = new List<SlabRow>();
        decimal taxable = Math.Max(0, annualIncome - 1_800_000m);

        slabs.Add(new SlabRow("Up to Rs. 1,800,000", "0%", Math.Min(annualIncome, 1_800_000m), 0));

        decimal s1 = Math.Min(taxable, 1_000_000m);
        slabs.Add(new SlabRow("Rs. 1,800,001 – Rs. 2,800,000", "6%", s1, s1 * 0.06m));
        taxable -= s1; if (taxable <= 0) return slabs;

        decimal s2 = Math.Min(taxable, 500_000m);
        slabs.Add(new SlabRow("Rs. 2,800,001 – Rs. 3,300,000", "18%", s2, s2 * 0.18m));
        taxable -= s2; if (taxable <= 0) return slabs;

        decimal s3 = Math.Min(taxable, 500_000m);
        slabs.Add(new SlabRow("Rs. 3,300,001 – Rs. 3,800,000", "24%", s3, s3 * 0.24m));
        taxable -= s3; if (taxable <= 0) return slabs;

        decimal s4 = Math.Min(taxable, 500_000m);
        slabs.Add(new SlabRow("Rs. 3,800,001 – Rs. 4,300,000", "30%", s4, s4 * 0.30m));
        taxable -= s4; if (taxable <= 0) return slabs;

        slabs.Add(new SlabRow("Above Rs. 4,300,000", "36%", taxable, taxable * 0.36m));
        return slabs;
    }

    private record SlabRow(string Band, string Rate, decimal TaxableAmount, decimal Tax);
}
