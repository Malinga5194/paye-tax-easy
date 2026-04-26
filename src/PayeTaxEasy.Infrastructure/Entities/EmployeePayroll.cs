namespace PayeTaxEasy.Infrastructure.Entities;

public class EmployeePayroll
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal GrossMonthlySalary { get; set; }
    public DateTime EmploymentStartDate { get; set; }
    public DateTime? EmploymentEndDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Employer Employer { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public ICollection<MonthlyDeduction> MonthlyDeductions { get; set; } = new List<MonthlyDeduction>();
}
