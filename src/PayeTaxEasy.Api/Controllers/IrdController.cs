using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayeTaxEasy.Infrastructure.Services;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("ird")]
[Authorize(Roles = "Employer,SystemAdmin")]
public class IrdController : ControllerBase
{
    private readonly IIrdIntegrationService _ird;

    public IrdController(IIrdIntegrationService ird) => _ird = ird;

    /// <summary>GET /ird/cumulative/{tin}/{financialYear} — Retrieve cumulative PAYE data from IRD</summary>
    [HttpGet("cumulative/{tin}/{financialYear}")]
    public async Task<IActionResult> GetCumulative(string tin, string financialYear)
    {
        var employerIdClaim = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();
        if (!Guid.TryParse(employerIdClaim, out var employerId))
            employerId = Guid.Empty;

        try
        {
            var result = await _ird.GetCumulativeDataAsync(tin, financialYear, employerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { errorCode = "IRD_001", message = ex.Message });
        }
    }
}
