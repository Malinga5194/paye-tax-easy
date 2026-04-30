using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayeTaxEasy.Infrastructure.Services;

public class EmployeePortalService : IEmployeePortalService
{
    private readonly PayeTaxEasyDbContext _db;
    private readonly IAuditService _audit;

    public EmployeePortalService(PayeTaxEasyDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<DeductionHistoryDto> GetDeductionHistoryAsync(
        string employeeTin, string financialYear)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.TIN == employeeTin)
            ?? throw new KeyNotFoundException($"Employee TIN {employeeTin} not found.");

        // Financial year: e.g. "2025-26" means Apr 2025 to Mar 2026
        int fyStartYear = GetFYStartYear(financialYear);
        var fyStart = new DateTime(fyStartYear, 4, 1);       // 1 April
        var fyEnd = new DateTime(fyStartYear + 1, 3, 31);    // 31 March

        var deductions = await _db.MonthlyDeductions
            .Include(d => d.EmployeePayroll)
                .ThenInclude(p => p.Employer)
            .Where(d => d.EmployeePayroll.Employee.TIN == employeeTin)
            .ToListAsync();

        // Filter in memory for correct FY boundary (Apr-Mar)
        var fyDeductions = deductions
            .Where(d =>
            {
                var deductionDate = new DateTime(d.Year, d.Month, 1);
                return deductionDate >= fyStart && deductionDate <= fyEnd;
            })
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToList();

        var entries = fyDeductions.Select(d => new DeductionEntryDto(
            d.EmployeePayroll.Employer.OrganizationName,
            d.MonthlyDeductionAmount,
            new DateTime(d.Year, d.Month, 1),
            d.Month, d.Year)).ToList();

        return new DeductionHistoryDto(
            employeeTin, employee.FullName, financialYear,
            entries, entries.Sum(e => e.DeductionAmount));
    }

    public async Task<byte[]> GeneratePdfAsync(string employeeTin, string financialYear)
    {
        var history = await GetDeductionHistoryAsync(employeeTin, financialYear);

        // Calculate tax figures for the notice
        var payroll = await _db.EmployeePayrolls
            .Include(p => p.MonthlyDeductions)
            .FirstOrDefaultAsync(p => p.Employee.TIN == employeeTin);

        decimal annualTax = 0m;
        decimal adjustedMonthly = 0m;
        decimal standardMonthly = 0m;
        int remainingMonths = 0;

        if (payroll != null)
        {
            var irdCache = await _db.IrdCumulativeCaches
                .Where(c => c.EmployeeTIN == employeeTin && c.FinancialYear == financialYear)
                .OrderByDescending(c => c.RetrievedAt)
                .FirstOrDefaultAsync();

            decimal priorDeduction = irdCache?.CumulativeDeduction ?? 0;
            decimal priorIncome = irdCache?.CumulativeIncome ?? 0;

            // Use actual income from monthly records (handles salary changes correctly)
            var allDeductions = payroll.MonthlyDeductions
                .OrderBy(d => d.Year).ThenBy(d => d.Month).ToList();

            decimal actualIncome = priorIncome + allDeductions.Sum(d => d.GrossIncome);
            annualTax = Core.Calculator.PayeCalculator.CalculateAnnualTax(actualIncome);
            standardMonthly = Math.Round(annualTax / 12, 0);

            // Remaining months after last recorded deduction
            var lastDeduction = allDeductions.LastOrDefault();
            if (lastDeduction != null)
            {
                var lastPeriod = new DateTime(lastDeduction.Year, lastDeduction.Month, 1);
                var fyEnd = new DateTime(2026, 3, 31);
                remainingMonths = Math.Max(0, ((fyEnd.Year - lastPeriod.Year) * 12) + fyEnd.Month - lastPeriod.Month);
            }

            decimal totalPaid = priorDeduction + allDeductions.Sum(d => d.MonthlyDeductionAmount);
            decimal remainingTax = Math.Max(0, annualTax - totalPaid);
            adjustedMonthly = remainingMonths > 0 ? Math.Round(remainingTax / remainingMonths, 0) : 0;
        }

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
                            c.Item().Text("Employee Tax Deduction History").FontSize(13).FontColor("#555555");
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Financial Year: {financialYear}").FontSize(9);
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
                                c.Item().Text($"Name: {history.EmployeeFullName}").Bold();
                                c.Item().Text($"TIN: {history.EmployeeTIN}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Financial Year: {financialYear}").Bold();
                                c.Item().Text($"Total PAYE Deducted: Rs. {history.CumulativeTotal:N0}").Bold().FontColor("#27ae60");
                            });
                        });
                    });

                    // Deduction Table
                    col.Item().PaddingTop(10).Text("Monthly Deduction History").Bold().FontSize(12).FontColor("#003366");
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background("#003366").Padding(6)
                                .Text("Employer").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#003366").Padding(6)
                                .Text("Period").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#003366").Padding(6)
                                .Text("Deduction (Rs.)").FontColor("#FFFFFF").Bold();
                        });
                        bool alt = false;
                        foreach (var entry in history.Entries)
                        {
                            var bg = alt ? "#f8f9fa" : "#ffffff";
                            table.Cell().Background(bg).Padding(5).Text(entry.EmployerName);
                            table.Cell().Background(bg).Padding(5).Text($"{entry.Month:D2}/{entry.Year}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"Rs. {entry.DeductionAmount:N0}");
                            alt = !alt;
                        }
                    });

                    col.Item().PaddingTop(8).Background("#003366").Padding(8).Row(r =>
                    {
                        r.RelativeItem().Text("Total PAYE Deducted").Bold().FontColor("#FFFFFF");
                        r.ConstantItem(150).AlignRight().Text($"Rs. {history.CumulativeTotal:N0}").Bold().FontColor("#FFFFFF");
                    });

                    // ── Important Notice ──────────────────────────────────────────
                    if (annualTax > 0)
                    {
                        col.Item().PaddingTop(15).Border(1.5f).BorderColor("#f59e0b")
                            .Background("#fffbea").Padding(12).Column(notice =>
                        {
                            notice.Item().Text("⚠  Important Notice — PAYE Tax Adjustment")
                                .Bold().FontSize(11).FontColor("#92400e");

                            notice.Item().PaddingTop(6).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(9).FontColor("#78350f"));
                                t.Span("Your monthly PAYE deduction has been ");
                                t.Span("adjusted").Bold().FontColor("#003366");
                                t.Span($" based on the cumulative tax already paid during financial year {financialYear}.");
                            });

                            notice.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                                {
                                    c.Item().Text("Total Tax Paid This FY").FontSize(8).FontColor("#92400e").Bold();
                                    c.Item().Text($"Rs. {history.CumulativeTotal:N0}").Bold().FontSize(12).FontColor("#27ae60");
                                });
                                row.ConstantItem(8);
                                row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                                {
                                    c.Item().Text("Adjusted Monthly Deduction (This FY)").FontSize(8).FontColor("#92400e").Bold();
                                    c.Item().Text($"Rs. {adjustedMonthly:N0}").Bold().FontSize(12).FontColor("#17a2b8");
                                });
                                row.ConstantItem(8);
                                row.RelativeItem().Background("#ffffff").Border(1).BorderColor("#fcd34d").Padding(8).Column(c =>
                                {
                                    c.Item().Text("Standard Monthly Deduction (Next FY)").FontSize(8).FontColor("#92400e").Bold();
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
                                t.Span("From the next financial year onwards, the standard monthly deduction of ");
                                t.Span($"Rs. {standardMonthly:N0}").Bold().FontColor("#003366");
                                t.Span(" will apply, calculated fresh without considering prior cumulative payments.");
                            });
                        });
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Generated on {DateTime.UtcNow:f} UTC | PAYE Tax Easy | Inland Revenue (Amendment) Act No. 02 of 2025");
            });
        });

        await _audit.RecordAsync(employeeTin, "Employee", "PDFExported",
            "DeductionHistory", employeeTin, financialYear);

        return pdf.GeneratePdf();
    }

    private static int GetFYStartYear(string fy) => int.Parse(fy.Split('-')[0]);
    private static int GetFYEndYear(string fy) => int.Parse(fy.Split('-')[0]) + 1;
}
