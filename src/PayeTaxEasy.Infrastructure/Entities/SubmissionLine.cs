namespace PayeTaxEasy.Infrastructure.Entities;

public class SubmissionLine
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public string EmployeeTIN { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public decimal MonthlyDeductionAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public PayrollSubmission Submission { get; set; } = null!;
}
