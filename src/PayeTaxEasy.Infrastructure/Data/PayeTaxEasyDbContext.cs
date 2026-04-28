using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Infrastructure.Data;

public class PayeTaxEasyDbContext : DbContext
{
    public PayeTaxEasyDbContext(DbContextOptions<PayeTaxEasyDbContext> options) : base(options) { }

    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeePayroll> EmployeePayrolls => Set<EmployeePayroll>();
    public DbSet<MonthlyDeduction> MonthlyDeductions => Set<MonthlyDeduction>();
    public DbSet<PayrollSubmission> PayrollSubmissions => Set<PayrollSubmission>();
    public DbSet<SubmissionLine> SubmissionLines => Set<SubmissionLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<IrdCumulativeCache> IrdCumulativeCaches => Set<IrdCumulativeCache>();
    public DbSet<SecurityAlertLog> SecurityAlertLogs => Set<SecurityAlertLog>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Employer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Employer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.TIN).HasMaxLength(20).IsRequired();
            e.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RegistrationNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(20);
            e.HasIndex(x => x.TIN).IsUnique();
            e.HasIndex(x => x.RegistrationNumber).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Employee ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.TIN).HasMaxLength(20).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.NICNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(20);
            e.HasIndex(x => x.TIN).IsUnique();
            e.HasIndex(x => x.NICNumber).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── EmployeePayroll ───────────────────────────────────────────────────
        modelBuilder.Entity<EmployeePayroll>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.GrossMonthlySalary).HasPrecision(18, 2).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Employer)
                .WithMany(x => x.EmployeePayrolls)
                .HasForeignKey(x => x.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee)
                .WithMany(x => x.EmployeePayrolls)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MonthlyDeduction ──────────────────────────────────────────────────
        modelBuilder.Entity<MonthlyDeduction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.GrossIncome).HasPrecision(18, 2);
            e.Property(x => x.AnnualTaxLiability).HasPrecision(18, 2);
            e.Property(x => x.MonthlyDeductionAmount).HasPrecision(18, 2);
            e.Property(x => x.CumulativeDeductionAtCalculation).HasPrecision(18, 2);
            e.Property(x => x.CalculationTrigger).HasMaxLength(50)
                .HasDefaultValue("InitialEntry");
            e.Property(x => x.CalculatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.EmployeePayrollId, x.Year, x.Month });
            e.HasOne(x => x.EmployeePayroll)
                .WithMany(x => x.MonthlyDeductions)
                .HasForeignKey(x => x.EmployeePayrollId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PayrollSubmission ─────────────────────────────────────────────────
        modelBuilder.Entity<PayrollSubmission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.FinancialYear).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
            e.Property(x => x.IRDReferenceNumber).HasMaxLength(50);
            e.Property(x => x.TotalPAYEAmount).HasPrecision(18, 2);
            e.Property(x => x.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Employer)
                .WithMany(x => x.PayrollSubmissions)
                .HasForeignKey(x => x.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SubmissionLine ────────────────────────────────────────────────────
        modelBuilder.Entity<SubmissionLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EmployeeTIN).HasMaxLength(20).IsRequired();
            e.Property(x => x.GrossSalary).HasPrecision(18, 2);
            e.Property(x => x.MonthlyDeductionAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Submission)
                .WithMany(x => x.SubmissionLines)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLog (append-only) ────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ActorId).HasMaxLength(100).IsRequired();
            e.Property(x => x.ActorRole).HasMaxLength(50).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            e.Property(x => x.FinancialYear).HasMaxLength(10);
            e.Property(x => x.IPAddress).HasMaxLength(50);
            e.Property(x => x.AdditionalData).HasMaxLength(2000);
            e.Property(x => x.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.ActorId, x.Timestamp });
            // No FK relationships — audit log is standalone and append-only
        });

        // ── NotificationLog ───────────────────────────────────────────────────
        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.RecipientId).HasMaxLength(100).IsRequired();
            e.Property(x => x.NotificationType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Channel).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ReferenceId).HasMaxLength(100);
            e.Property(x => x.SentAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── IrdCumulativeCache (insert-only) ──────────────────────────────────
        modelBuilder.Entity<IrdCumulativeCache>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EmployeeTIN).HasMaxLength(20).IsRequired();
            e.Property(x => x.FinancialYear).HasMaxLength(10).IsRequired();
            e.Property(x => x.CumulativeIncome).HasPrecision(18, 2);
            e.Property(x => x.CumulativeDeduction).HasPrecision(18, 2);
            e.Property(x => x.RetrievedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.EmployeeTIN, x.FinancialYear });
            e.HasOne(x => x.RetrievedByEmployer)
                .WithMany()
                .HasForeignKey(x => x.RetrievedByEmployerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AppUser ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TIN).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── SecurityAlertLog ──────────────────────────────────────────────────
        modelBuilder.Entity<SecurityAlertLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.AttemptedAction).HasMaxLength(200).IsRequired();
            e.Property(x => x.AttemptedBy).HasMaxLength(100);
            e.Property(x => x.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
