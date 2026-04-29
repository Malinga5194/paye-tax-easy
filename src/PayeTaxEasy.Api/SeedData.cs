using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Core.Calculator;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Api;

public static class SeedData
{
    public static void Seed(PayeTaxEasyDbContext db)
    {
        try
        {
            // Skip if already seeded
            if (db.Employees.Any()) return;

            const string FY = "2025-26";
            const string PASSWORD = "Test@1234";

            // ── Employer ──────────────────────────────────────────────────────
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                TIN = "EMP001TIN",
                OrganizationName = "ABC Company Ltd",
                RegistrationNumber = "REG001",
                ContactEmail = "employer@test.com",
                ContactPhone = "+94112345678",
                SMSNotificationsEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Employers.Add(employer);

            // ── 8 Employees with logins ───────────────────────────────────────
            var employees = new[]
            {
                // 1. Below relief — full year stable (Rs. 150,000/month = Rs. 1,800,000/year → 0% tax)
                new { TIN = "EMP100001V", Name = "Kamal Perera",       NIC = "900123456V", Email = "kamal.perera@test.com",       Phone = "+94771000001", Salary = 150_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)null,              Scenario = "Below Tax Relief - Full Year" },
                // 2. 6% slab — full year stable (Rs. 250,000/month = Rs. 3,000,000/year)
                new { TIN = "EMP100002V", Name = "Nimal Silva",        NIC = "850234567V", Email = "nimal.silva@test.com",        Phone = "+94771000002", Salary = 250_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)null,              Scenario = "6% Tax Slab - Full Year" },
                // 3. Recently joined Oct 2025 (Rs. 320,000/month — adjusted deduction)
                new { TIN = "EMP100003V", Name = "Priya Jayasinghe",   NIC = "920345678V", Email = "priya.jayasinghe@test.com",   Phone = "+94771000003", Salary = 320_000m,  StartDate = new DateTime(2025, 10, 1), EndDate = (DateTime?)null,              Scenario = "Recently Joined - Adjusted Deduction" },
                // 4. Salary increase — started Rs. 200,000, increased to Rs. 350,000 from Aug 2025
                new { TIN = "EMP100004V", Name = "Amali Fernando",     NIC = "880456789V", Email = "amali.fernando@test.com",     Phone = "+94771000004", Salary = 350_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)null,              Scenario = "Salary Increase Mid-Year" },
                // 5. Salary decrease — started Rs. 400,000, decreased to Rs. 280,000 from Sep 2025
                new { TIN = "EMP100005V", Name = "Suresh Bandara",     NIC = "870567890V", Email = "suresh.bandara@test.com",     Phone = "+94771000005", Salary = 280_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)null,              Scenario = "Salary Decrease Mid-Year" },
                // 6. Changed employer — joined this company Nov 2025 (prior deductions from IRD)
                new { TIN = "EMP100006V", Name = "Roshan Wickrama",    NIC = "860678901V", Email = "roshan.wickrama@test.com",    Phone = "+94771000006", Salary = 500_000m,  StartDate = new DateTime(2025, 11, 1), EndDate = (DateTime?)null,              Scenario = "Changed Employer - Prior IRD Deductions" },
                // 7. Resigned Dec 2025 (left mid-year)
                new { TIN = "EMP100007V", Name = "Dilani Rathnayake",  NIC = "950789012V", Email = "dilani.rathnayake@test.com",  Phone = "+94771000007", Salary = 180_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)new DateTime(2025, 12, 31), Scenario = "Resigned Mid-Year" },
                // 8. High earner — 36% slab (Rs. 600,000/month = Rs. 7,200,000/year)
                new { TIN = "EMP100008V", Name = "Chamara Dissanayake",NIC = "800890123V", Email = "chamara.dissanayake@test.com",Phone = "+94771000008", Salary = 600_000m,  StartDate = new DateTime(2025, 4, 1),  EndDate = (DateTime?)null,              Scenario = "High Earner - 36% Slab" },
            };

            foreach (var e in employees)
            {
                // Create Employee record
                var emp = new Employee
                {
                    Id = Guid.NewGuid(),
                    TIN = e.TIN,
                    FullName = e.Name,
                    NICNumber = e.NIC,
                    ContactEmail = e.Email,
                    ContactPhone = e.Phone,
                    CreatedAt = DateTime.UtcNow
                };
                db.Employees.Add(emp);

                // Create login for employee
                db.AppUsers.Add(new AppUser
                {
                    Email = e.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(PASSWORD),
                    Role = "Employee",
                    FullName = e.Name,
                    TIN = e.TIN,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                // Create EmployeePayroll record
                var payroll = new EmployeePayroll
                {
                    Id = Guid.NewGuid(),
                    EmployerId = employer.Id,
                    EmployeeId = emp.Id,
                    GrossMonthlySalary = e.Salary,
                    EmploymentStartDate = e.StartDate,
                    EmploymentEndDate = e.EndDate,
                    EffectiveDate = e.StartDate,
                    IsActive = e.EndDate == null || e.EndDate > DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                db.EmployeePayrolls.Add(payroll);

                // Generate monthly deductions from start date to end date (or March 2026)
                var endDate = e.EndDate ?? new DateTime(2026, 3, 31);
                var current = new DateTime(e.StartDate.Year, e.StartDate.Month, 1);

                // For salary increase scenario (Amali) — use lower salary for Apr-Jul
                decimal salaryForCalc = e.Salary;
                if (e.TIN == "EMP100004V") salaryForCalc = 200_000m; // will be updated from Aug

                // For salary decrease scenario (Suresh) — use higher salary for Apr-Aug
                if (e.TIN == "EMP100005V") salaryForCalc = 400_000m; // will be updated from Sep

                // For changed employer (Roshan) — simulate prior deductions
                decimal cumulativeDeduction = 0m;
                decimal cumulativeIncome = 0m;
                if (e.TIN == "EMP100006V")
                {
                    // Prior employer paid Apr-Oct (7 months at Rs. 500,000)
                    cumulativeIncome = 500_000m * 7;
                    cumulativeDeduction = PayeCalculator.CalculateAnnualTax(500_000m * 12) / 12 * 7;

                    // Store IRD cache
                    db.IrdCumulativeCaches.Add(new IrdCumulativeCache
                    {
                        EmployeeTIN = e.TIN,
                        FinancialYear = FY,
                        CumulativeIncome = cumulativeIncome,
                        CumulativeDeduction = cumulativeDeduction,
                        RetrievedAt = DateTime.UtcNow,
                        RetrievedByEmployerId = employer.Id
                    });
                }

                while (current <= endDate && current <= new DateTime(2026, 3, 31))
                {
                    // Salary change logic
                    if (e.TIN == "EMP100004V" && current >= new DateTime(2025, 8, 1))
                        salaryForCalc = 350_000m; // salary increase from Aug
                    if (e.TIN == "EMP100005V" && current >= new DateTime(2025, 9, 1))
                        salaryForCalc = 280_000m; // salary decrease from Sep

                    // Calculate remaining months in FY
                    var fyEnd = new DateTime(2026, 3, 31);
                    int remainingMonths = ((fyEnd.Year - current.Year) * 12) + fyEnd.Month - current.Month + 1;
                    remainingMonths = Math.Max(1, Math.Min(12, remainingMonths));

                    // Project annual income
                    decimal projectedAnnual = cumulativeIncome + (salaryForCalc * remainingMonths);
                    decimal annualTax = PayeCalculator.CalculateAnnualTax(projectedAnnual);
                    decimal monthlyDeduction = PayeCalculator.CalculateAdjustedDeduction(
                        projectedAnnual, cumulativeDeduction, remainingMonths);

                    db.MonthlyDeductions.Add(new MonthlyDeduction
                    {
                        Id = Guid.NewGuid(),
                        EmployeePayrollId = payroll.Id,
                        Month = current.Month,
                        Year = current.Year,
                        GrossIncome = salaryForCalc,
                        AnnualTaxLiability = annualTax,
                        MonthlyDeductionAmount = monthlyDeduction,
                        CumulativeDeductionAtCalculation = cumulativeDeduction,
                        RemainingMonthsAtCalculation = remainingMonths,
                        CalculationTrigger = current == new DateTime(e.StartDate.Year, e.StartDate.Month, 1)
                            ? "InitialEntry" : "SalaryAdjustment",
                        IsOverpaid = false,
                        IsLocked = current < DateTime.UtcNow.AddMonths(-1),
                        CalculatedAt = current
                    });

                    // Accumulate for next month
                    cumulativeIncome += salaryForCalc;
                    cumulativeDeduction += monthlyDeduction;

                    current = current.AddMonths(1);
                }
            }

            // ── Employer login (update existing dev user) ─────────────────────
            if (!db.AppUsers.AsEnumerable().Any(u => u.Email == "employer@test.com"))
            {
                db.AppUsers.Add(new AppUser
                {
                    Email = "employer@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role = "Employer",
                    FullName = "ABC Company Ltd",
                    TIN = "EMP001TIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Error: {ex.Message}");
        }
    }
}
