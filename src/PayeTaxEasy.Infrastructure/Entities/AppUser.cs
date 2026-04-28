namespace PayeTaxEasy.Infrastructure.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Employer, Employee, IRD_Officer, SystemAdmin
    public string FullName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
