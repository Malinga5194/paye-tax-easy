using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface IEmployeePortalService
{
    Task<DeductionHistoryDto> GetDeductionHistoryAsync(string employeeTin, string financialYear);
    Task<byte[]> GeneratePdfAsync(string employeeTin, string financialYear);
}
