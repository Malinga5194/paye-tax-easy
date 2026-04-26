using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayeTaxEasy.Infrastructure.Services;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("employee")]
[Authorize(Roles = "Employee")]
public class EmployeePortalController : ControllerBase
{
    private readonly IEmployeePortalService _portal;

    public EmployeePortalController(IEmployeePortalService portal) => _portal = portal;

    /// <summary>GET /employee/history/{financialYear} — Tax deduction history</summary>
    [HttpGet("history/{financialYear}")]
    public async Task<IActionResult> GetHistory(string financialYear)
    {
        var tin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        try
        {
            var history = await _portal.GetDeductionHistoryAsync(tin, financialYear);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "No deduction records found for this financial year." });
        }
    }

    /// <summary>GET /employee/history/{financialYear}/pdf — Download PDF</summary>
    [HttpGet("history/{financialYear}/pdf")]
    public async Task<IActionResult> GetPdf(string financialYear)
    {
        var tin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        try
        {
            var pdf = await _portal.GeneratePdfAsync(tin, financialYear);
            return File(pdf, "application/pdf",
                $"PAYE_History_{tin}_{financialYear}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "No data available for export for this financial year." });
        }
    }
}
