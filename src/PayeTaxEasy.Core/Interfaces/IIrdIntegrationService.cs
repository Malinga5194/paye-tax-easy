using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface IIrdIntegrationService
{
    Task<CumulativeDataDto> GetCumulativeDataAsync(string employeeTin, string financialYear, Guid requestingEmployerId);
    Task<IrdSubmissionAckDto> SubmitToIrdAsync(Guid submissionId);
}
