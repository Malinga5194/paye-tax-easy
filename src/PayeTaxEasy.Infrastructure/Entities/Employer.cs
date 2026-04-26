namespace PayeTaxEasy.Infrastructure.Entities;

public class Employer
{
    public Guid Id { get; set; }
    public string TIN { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool SMSNotificationsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EmployeePayroll> EmployeePayrolls { get; set; } = new List<EmployeePayroll>();
    public ICollection<PayrollSubmission> PayrollSubmissions { get; set; } = new List<PayrollSubmission>();
}
