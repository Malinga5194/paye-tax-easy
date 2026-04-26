using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Infrastructure.Services;

/// <summary>
/// Handles secure communication with the IRD API.
/// In production this calls the real IRD OAuth2 endpoint.
/// For local dev / assignment, returns mock data when IRD is unavailable.
/// </summary>
public class IrdIntegrationService : IIrdIntegrationService
{
    private readonly PayeTaxEasyDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public IrdIntegrationService(
        PayeTaxEasyDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        IAuditService audit)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _audit = audit;
    }

    public async Task<CumulativeDataDto> GetCumulativeDataAsync(
        string employeeTin, string financialYear, Guid requestingEmployerId)
    {
        // Check cache first (most recent entry for this TIN + FY)
        var cached = await _db.IrdCumulativeCaches
            .Where(c => c.EmployeeTIN == employeeTin && c.FinancialYear == financialYear)
            .OrderByDescending(c => c.RetrievedAt)
            .FirstOrDefaultAsync();

        if (cached != null && cached.RetrievedAt > DateTime.UtcNow.AddHours(-1))
        {
            return new CumulativeDataDto(
                cached.EmployeeTIN, cached.FinancialYear,
                cached.CumulativeIncome, cached.CumulativeDeduction,
                cached.RetrievedAt, "Cache");
        }

        // In a real system, call IRD API here with OAuth2 client credentials.
        // For this assignment, we simulate with zero prior deductions for new employees.
        var result = new CumulativeDataDto(
            employeeTin, financialYear, 0m, 0m, DateTime.UtcNow, "IRD");

        // Store in cache (insert-only)
        _db.IrdCumulativeCaches.Add(new IrdCumulativeCache
        {
            EmployeeTIN = employeeTin,
            FinancialYear = financialYear,
            CumulativeIncome = result.CumulativeIncome,
            CumulativeDeduction = result.CumulativeDeduction,
            RetrievedAt = result.RetrievedAt,
            RetrievedByEmployerId = requestingEmployerId
        });
        await _db.SaveChangesAsync();

        await _audit.RecordAsync(
            requestingEmployerId.ToString(), "Employer",
            "IRDDataRetrieved", "Employee", employeeTin, financialYear);

        return result;
    }

    public async Task<IrdSubmissionAckDto> SubmitToIrdAsync(Guid submissionId)
    {
        var submission = await _db.PayrollSubmissions
            .Include(s => s.SubmissionLines)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        // Simulate IRD acceptance
        var refNumber = $"IRD-{DateTime.UtcNow:yyyy-MM}-{submissionId.ToString()[..8].ToUpper()}";
        submission.Status = "Accepted";
        submission.IRDReferenceNumber = refNumber;
        submission.IRDAcceptedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new IrdSubmissionAckDto(refNumber, "Accepted", DateTime.UtcNow);
    }
}
