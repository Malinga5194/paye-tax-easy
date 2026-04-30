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

        // Use ALL deductions for the full year (consistent regardless of selected month)
        var allDeductions = payroll.MonthlyDeductions
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToList();

        // Calculate actual total income for the full year (handles salary changes correctly)
        decimal actualIncome = priorEmployerIncome + allDeductions.Sum(d => d.GrossIncome);
        decimal annualTaxLiability = PayeCalculator.CalculateAnnualTax(actualIncome);

        // Total tax paid so far (all months)
        decimal currentEmployerYTD = allDeductions.Sum(d => d.MonthlyDeductionAmount);
        decimal totalYTD = priorEmployerDeduction + currentEmployerYTD;

        // Remaining months based on active months in the year
        int totalActiveMonths = allDeductions.Count;
        int remainingMonths = Math.Max(0, 12 - totalActiveMonths);

        decimal remainingTax = Math.Max(0, annualTaxLiability - totalYTD);
        decimal adjustedMonthly = remainingMonths > 0
            ? Math.Round(remainingTax / remainingMonths, 0)
            : 0;
        decimal projectedAnnual = actualIncome;

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
            taxRelief = 1_800_000m,
            taxableIncome = Math.Max(0, projectedAnnual - 1_800_000m),

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

            // Monthly history
            monthlyHistory = allDeductions.Select(d => new
            {
                month = $"{d.Year}-{d.Month:D2}",
                monthLabel = new DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy"),
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

        // Use ALL deductions for the full year (consistent values)
        var allDeductions = payroll.MonthlyDeductions
            .OrderBy(d => d.Year).ThenBy(d => d.Month).ToList();

        decimal actualIncome = priorIncome + allDeductions.Sum(d => d.GrossIncome);
        decimal annualTax = PayeCalculator.CalculateAnnualTax(actualIncome);
        decimal currentYTD = allDeductions.Sum(d => d.MonthlyDeductionAmount);
        decimal totalYTD = priorDeduction + currentYTD;
        int totalActiveMonths = allDeductions.Count;
        int remainingMonths = Math.Max(0, 12 - totalActiveMonths);
        decimal remainingTax = Math.Max(0, annualTax - totalYTD);
        decimal adjustedMonthly = remainingMonths > 0 ? Math.Round(remainingTax / remainingMonths, 0) : 0;
        decimal projectedAnnual = actualIncome;

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
                        Row("Tax Already Paid (Prior Employer)", $"Rs. {priorDeduction:N0}");
                        Row("Tax Paid (Current Employer YTD)", $"Rs. {currentYTD:N0}");
                        Row("Total Tax Paid to Date", $"Rs. {totalYTD:N0}", true, "#27ae60");
                        Row("Remaining Tax for Financial Year", $"Rs. {remainingTax:N0}", true, "#e67e22");
                        Row("Remaining Months in FY", remainingMonths.ToString());
                        Row("Adjusted Monthly Deduction", $"Rs. {adjustedMonthly:N0}", true, "#003366");
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
                        notice.Item().Text("⚠  Important Notice — PAYE Tax Adjustment")
                            .Bold().FontSize(11).FontColor("#92400e");
                        notice.Item().PaddingTop(6).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(9).FontColor("#78350f"));
                            t.Span("The monthly PAYE deduction for this employee has been ");
                            t.Span("adjusted").Bold().FontColor("#003366");
                            t.Span($" based on the cumulative tax already paid during financial year {financialYear}.");
                        });

                        notice.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Cumulative Tax Paid to Date").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {totalYTD:N0}").Bold().FontSize(13).FontColor("#27ae60");
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Adjusted Monthly Deduction (This FY)").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {adjustedMonthly:N0}").Bold().FontSize(13).FontColor("#17a2b8");
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                            {
                                c.Item().Text("Standard Monthly Deduction (Next FY)").FontSize(8).FontColor("#92400e").Bold();
                                c.Item().Text($"Rs. {Math.Round(annualTax / 12):N0}").Bold().FontSize(13).FontColor("#003366");
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
                            t.Span("From the next financial year onwards, the standard monthly deduction of ");
                            t.Span($"Rs. {Math.Round(annualTax / 12):N0}").Bold().FontColor("#003366");
                            t.Span(" will apply, calculated fresh without considering prior cumulative payments.");
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
