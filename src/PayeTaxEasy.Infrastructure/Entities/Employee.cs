namespace PayeTaxEasy.Infrastructure.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string TIN { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NICNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<EmployeePayroll> EmployeePayrolls { get; set; } = new List<EmployeePayroll>();
}
