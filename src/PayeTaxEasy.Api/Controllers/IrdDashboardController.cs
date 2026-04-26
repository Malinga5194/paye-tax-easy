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

    public IrdDashboardController(IIrdDashboardService dashboard, IAuditService audit)
    {
        _dashboard = dashboard;
        _audit = audit;
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
}
