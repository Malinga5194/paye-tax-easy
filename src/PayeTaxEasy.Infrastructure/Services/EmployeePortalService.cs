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

        var deductions = await _db.MonthlyDeductions
            .Include(d => d.EmployeePayroll)
                .ThenInclude(p => p.Employer)
            .Where(d => d.EmployeePayroll.Employee.TIN == employeeTin
                && d.Year >= GetFYStartYear(financialYear)
                && d.Year <= GetFYEndYear(financialYear))
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToListAsync();

        var entries = deductions.Select(d => new DeductionEntryDto(
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

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("PAYE Tax Easy").Bold().FontSize(18);
                    col.Item().Text("Tax Deduction History").FontSize(14);
                    col.Item().Text($"Financial Year: {financialYear}").FontSize(12);
                    col.Item().LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).Text($"Employee: {history.EmployeeFullName}").Bold();
                    col.Item().Text($"TIN: {history.EmployeeTIN}");
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background("#003366").Padding(5)
                                .Text("Employer").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#003366").Padding(5)
                                .Text("Period").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#003366").Padding(5)
                                .Text("Deduction (Rs.)").FontColor("#FFFFFF").Bold();
                        });
                        foreach (var entry in history.Entries)
                        {
                            table.Cell().Padding(5).Text(entry.EmployerName);
                            table.Cell().Padding(5).Text($"{entry.Month:D2}/{entry.Year}");
                            table.Cell().Padding(5).Text($"{entry.DeductionAmount:N2}");
                        }
                    });
                    col.Item().PaddingTop(10)
                        .Text($"Total PAYE Deducted: Rs. {history.CumulativeTotal:N2}").Bold();
                });

                page.Footer().AlignCenter()
                    .Text($"Generated on {DateTime.UtcNow:f} UTC | PAYE Tax Easy");
            });
        });

        await _audit.RecordAsync(employeeTin, "Employee", "PDFExported",
            "DeductionHistory", employeeTin, financialYear);

        return pdf.GeneratePdf();
    }

    private static int GetFYStartYear(string fy) => int.Parse(fy.Split('-')[0]);
    private static int GetFYEndYear(string fy) => int.Parse(fy.Split('-')[0]) + 1;
}
