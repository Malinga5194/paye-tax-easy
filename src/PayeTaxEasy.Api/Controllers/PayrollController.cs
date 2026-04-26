using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Services;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("payroll")]
[Authorize(Roles = "Employer,SystemAdmin")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payroll;

    public PayrollController(IPayrollService payroll) => _payroll = payroll;

    /// <summary>POST /payroll/employees/{tin}/salary — Add salary record and calculate PAYE</summary>
    [HttpPost("employees/{tin}/salary")]
    public async Task<IActionResult> AddSalary(string tin, [FromBody] AddSalaryRequest req)
    {
        if (req.GrossMonthlySalary <= 0)
            return UnprocessableEntity(new ErrorResponse("PAYROLL_001",
                "Gross monthly salary must be a positive value in Sri Lankan Rupees.",
                "grossMonthlySalary", "Enter a numeric value greater than 0."));

        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;

        try
        {
            var result = await _payroll.AddSalaryAsync(
                employerTin, tin, req.GrossMonthlySalary,
                req.EmploymentStartDate, req.FinancialYear);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return UnprocessableEntity(new ErrorResponse("PAYROLL_002", ex.Message,
                "employeeTIN", "Verify the employee TIN is registered in the system."));
        }
    }

    /// <summary>PUT /payroll/employees/{tin}/salary/{id} — Update salary (correction)</summary>
    [HttpPut("employees/{tin}/salary/{id:guid}")]
    public async Task<IActionResult> UpdateSalary(string tin, Guid id, [FromBody] UpdateSalaryRequest req)
    {
        try
        {
            var result = await _payroll.UpdateSalaryAsync(id, req.GrossMonthlySalary, req.EffectiveDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("PAYROLL_004"))
        {
            return Conflict(new ErrorResponse("PAYROLL_004", "Payroll period is locked.",
                "period", "Contact a SystemAdmin to authorize a correction."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("PAYROLL_002", ex.Message, "id", "Verify the salary record ID."));
        }
    }

    /// <summary>GET /payroll/employees/{tin}/salary — Salary history</summary>
    [HttpGet("employees/{tin}/salary")]
    public async Task<IActionResult> GetSalaryHistory(string tin)
    {
        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var history = await _payroll.GetSalaryHistoryAsync(employerTin, tin);
        return Ok(history);
    }

    /// <summary>GET /payroll/summary/{period} — Deduction summary for a payroll period</summary>
    [HttpGet("summary/{period}")]
    public async Task<IActionResult> GetSummary(string period)
    {
        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var summary = await _payroll.GetDeductionSummaryAsync(employerTin, period);
        return Ok(summary);
    }

    /// <summary>POST /payroll/summary/{period}/finalize — Lock payroll period</summary>
    [HttpPost("summary/{period}/finalize")]
    public async Task<IActionResult> FinalizePeriod(string period)
    {
        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        await _payroll.FinalizePeriodAsync(employerTin, period);
        return Ok(new { message = $"Period {period} finalized and locked." });
    }

    /// <summary>POST /payroll/submissions — Submit PAYE return</summary>
    [HttpPost("submissions")]
    public async Task<IActionResult> Submit([FromBody] PayrollSubmissionRequestDto req)
    {
        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        try
        {
            var result = await _payroll.SubmitPayrollAsync(employerTin, req);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("PAYROLL_003"))
        {
            return UnprocessableEntity(new ErrorResponse("PAYROLL_003",
                "The PAYE filing deadline has passed.", "financialYear",
                "Contact IRD for an authorized extension."));
        }
    }

    /// <summary>POST /payroll/submissions/bulk — Bulk PAYE submission</summary>
    [HttpPost("submissions/bulk")]
    public async Task<IActionResult> SubmitBulk([FromBody] BulkPayrollSubmissionRequestDto req)
    {
        var employerTin = User.FindFirst("extension_TIN")?.Value
            ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var result = await _payroll.SubmitBulkPayrollAsync(employerTin, req);
        return Ok(result);
    }
}

public record AddSalaryRequest(
    decimal GrossMonthlySalary,
    DateTime EmploymentStartDate,
    string FinancialYear);

public record UpdateSalaryRequest(
    decimal GrossMonthlySalary,
    DateTime EffectiveDate);

public record ErrorResponse(
    string ErrorCode,
    string Message,
    string Field,
    string SuggestedAction);
