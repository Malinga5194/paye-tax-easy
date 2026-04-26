namespace PayeTaxEasy.Infrastructure.Entities;

public class IrdCumulativeCache
{
    public Guid Id { get; set; }
    public string EmployeeTIN { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public decimal CumulativeIncome { get; set; }
    public decimal CumulativeDeduction { get; set; }
    public DateTime RetrievedAt { get; set; }
    public Guid RetrievedByEmployerId { get; set; }

    public Employer RetrievedByEmployer { get; set; } = null!;
}
