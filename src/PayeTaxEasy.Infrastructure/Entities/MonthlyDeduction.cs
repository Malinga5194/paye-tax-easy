namespace PayeTaxEasy.Infrastructure.Entities;

public class MonthlyDeduction
{
    public Guid Id { get; set; }
    public Guid EmployeePayrollId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal AnnualTaxLiability { get; set; }
    public decimal MonthlyDeductionAmount { get; set; }
    public decimal CumulativeDeductionAtCalculation { get; set; }
    public int RemainingMonthsAtCalculation { get; set; }
    public string CalculationTrigger { get; set; } = "InitialEntry";
    public bool IsOverpaid { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CalculatedAt { get; set; }

    public EmployeePayroll EmployeePayroll { get; set; } = null!;
}
