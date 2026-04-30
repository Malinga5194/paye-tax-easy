using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Services;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("ird-dashboard")]
[Authorize(Roles = "IRD_Officer,SystemAdmin")]
public class IrdDashboardController : ControllerBase
{
    private readonly IIrdDashboardService _dashboard;
    private readonly IAuditService _audit;
    private readonly IIrdTinSearchService _tinSearch;

    public IrdDashboardController(IIrdDashboardService dashboard, IAuditService audit, IIrdTinSearchService tinSearch)
    {
        _dashboard = dashboard;
        _audit = audit;
        _tinSearch = tinSearch;
    }

    /// <summary>GET /ird-dashboard/compliance/{financialYear}</summary>
    [HttpGet("compliance/{financialYear}")]
    public async Task<IActionResult> GetCompliance(string financialYear)
    {
        var report = await _dashboard.GetComplianceReportAsync(financialYear);
        return Ok(report);
    }

    /// <summary>GET /ird-dashboard/compliance/{financialYear}/export — CSV export</summary>
    [HttpGet("compliance/{financialYear}/export")]
    public async Task<IActionResult> ExportCompliance(string financialYear)
    {
        var actorId = User.FindFirst("sub")?.Value ?? "unknown";
        var csv = await _dashboard.ExportComplianceReportAsync(financialYear);
        await _audit.RecordAsync(actorId, "IRD_Officer", "ComplianceReportExported",
            "ComplianceReport", financialYear, financialYear);
        return File(csv, "text/csv", $"compliance_{financialYear}.csv");
    }

    /// <summary>GET /ird-dashboard/employers/{registrationNo}</summary>
    [HttpGet("employers/{registrationNo}")]
    public async Task<IActionResult> GetEmployer(string registrationNo)
    {
        try
        {
            var detail = await _dashboard.GetEmployerDetailAsync(registrationNo);
            return Ok(detail);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Employer {registrationNo} not found." });
        }
    }

    /// <summary>GET /ird-dashboard/audit-logs — Paginated audit log query</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _dashboard.GetAuditLogsAsync(query);
        return Ok(result);
    }

    /// <summary>GET /ird-dashboard/employers — List all employers with submission summary</summary>
    [HttpGet("employers")]
    public async Task<IActionResult> GetAllEmployers([FromQuery] string financialYear = "2025-26")
    {
        var result = await _dashboard.GetAllEmployersWithSubmissionsAsync(financialYear);
        return Ok(result);
    }

    /// <summary>GET /ird-dashboard/employees — List all employees with tax summary</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetAllEmployees([FromQuery] string financialYear = "2025-26")
    {
        var result = await _dashboard.GetAllEmployeesWithTaxAsync(financialYear);
        return Ok(result);
    }

    /// <summary>GET /ird-dashboard/search/employee/{tin} — Search employee by TIN</summary>
    [HttpGet("search/employee/{tin}")]
    public async Task<IActionResult> SearchEmployeeByTin(string tin, [FromQuery] string financialYear = "2025-26")
    {
        var actorId = User.FindFirst("sub")?.Value ?? "unknown";
        var result = await _tinSearch.SearchEmployeeByTinAsync(tin, financialYear);
        if (result == null)
            return NotFound(new { message = $"No employee found with TIN {tin}" });

        await _audit.RecordAsync(actorId, "IRD_Officer", "EmployeeTINSearchPerformed",
            "Employee", tin, financialYear);
        return Ok(result);
    }

    /// <summary>GET /ird-dashboard/search/employee/{tin}/pdf — Generate employee PDF report</summary>
    [HttpGet("search/employee/{tin}/pdf")]
    public async Task<IActionResult> GenerateEmployeePdf(string tin, [FromQuery] string financialYear = "2025-26")
    {
        var actorId = User.FindFirst("sub")?.Value ?? "unknown";
        var pdfBytes = await _tinSearch.GenerateEmployeePdfAsync(tin, financialYear);
        await _audit.RecordAsync(actorId, "IRD_Officer", "EmployeePDFReportGenerated",
            "Employee", tin, financialYear);
        return File(pdfBytes, "application/pdf", $"IRD_Employee_Report_{tin}_{financialYear}.pdf");
    }

    /// <summary>GET /ird-dashboard/search/employer/{tin} — Search employer by TIN</summary>
    [HttpGet("search/employer/{tin}")]
    public async Task<IActionResult> SearchEmployerByTin(string tin, [FromQuery] string financialYear = "2025-26")
    {
        var actorId = User.FindFirst("sub")?.Value ?? "unknown";
        var result = await _tinSearch.SearchEmployerByTinAsync(tin, financialYear);
        if (result == null)
            return NotFound(new { message = $"No employer found with TIN {tin}" });

        await _audit.RecordAsync(actorId, "IRD_Officer", "EmployerTINSearchPerformed",
            "Employer", tin, financialYear);
        return Ok(result);
    }

    /// <summary>GET /ird-dashboard/search/employer/{tin}/pdf — Generate employer PDF report</summary>
    [HttpGet("search/employer/{tin}/pdf")]
    public async Task<IActionResult> GenerateEmployerPdf(string tin, [FromQuery] string financialYear = "2025-26")
    {
        var actorId = User.FindFirst("sub")?.Value ?? "unknown";
        var pdfBytes = await _tinSearch.GenerateEmployerPdfAsync(tin, financialYear);
        await _audit.RecordAsync(actorId, "IRD_Officer", "EmployerPDFReportGenerated",
            "Employer", tin, financialYear);
        return File(pdfBytes, "application/pdf", $"IRD_Employer_Report_{tin}_{financialYear}.pdf");
    }
}
