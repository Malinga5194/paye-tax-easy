using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface IIrdTinSearchService
{
    Task<EmployeeTinSearchResultDto?> SearchEmployeeByTinAsync(string tin, string financialYear);
    Task<byte[]> GenerateEmployeePdfAsync(string tin, string financialYear);
    Task<EmployerTinSearchResultDto?> SearchEmployerByTinAsync(string tin, string financialYear);
    Task<byte[]> GenerateEmployerPdfAsync(string tin, string financialYear);
}
