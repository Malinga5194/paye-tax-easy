namespace PayeTaxEasy.Infrastructure.Entities;

public class PayrollSubmission
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? IRDReferenceNumber { get; set; }
    public decimal TotalPAYEAmount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? IRDAcceptedAt { get; set; }

    public Employer Employer { get; set; } = null!;
    public ICollection<SubmissionLine> SubmissionLines { get; set; } = new List<SubmissionLine>();
}
