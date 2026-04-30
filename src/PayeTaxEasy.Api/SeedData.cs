using PayeTaxEasy.Core.Calculator;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Api;

/// <summary>
/// Seeds the database with realistic PAYE tax scenarios matching the assignment requirements.
/// Each scenario demonstrates how PAYE Tax Easy handles real-world employment situations.
/// </summary>
public static class SeedData
{
    public static void Seed(PayeTaxEasyDbContext db)
    {
        try
        {
            if (db.Employees.Any()) return;

            const string FY = "2025-26";
            const string PWD = "Test@1234";

            // ── Two Employers ─────────────────────────────────────────────────
            var companyY = new Employer
            {
                Id = Guid.NewGuid(), TIN = "200000001",
                OrganizationName = "ABC Company Ltd (Company Y)",
                RegistrationNumber = "REG001", ContactEmail = "employer@test.com",
                ContactPhone = "+94112345678", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            db.Employers.Add(companyY);

            // Employer login
            db.AppUsers.Add(new AppUser
            {
                Email = "employer@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(PWD),
                Role = "Employer", FullName = "ABC Company Ltd", TIN = "200000001",
                IsActive = true, CreatedAt = DateTime.UtcNow
            });

            // ── Define monthly salary schedules per employee ──────────────────
            // Each entry: (TIN, Name, NIC, Email, Phone, monthly salary schedule, prior employer data)
            var scenarios = new[]
            {
                // ═══════════════════════════════════════════════════════════════
                // Employee A — Stable Employment
                // Rs. 200,000/month × 12 = Rs. 2,400,000 annual
                // Taxable: Rs. 600,000 at 6% = Rs. 36,000 annual tax
                // Monthly deduction: Rs. 3,000
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000001", "Amal Perera", "900100001V",
                    "amal.perera@test.com", "+94771000001",
                    new DateTime(2025, 4, 1), null,
                    new[] {
                        (4, 200_000m), (5, 200_000m), (6, 200_000m), (7, 200_000m),
                        (8, 200_000m), (9, 200_000m), (10, 200_000m), (11, 200_000m),
                        (12, 200_000m), (1, 200_000m), (2, 200_000m), (3, 200_000m)
                    },
                    0m, 0m, "Stable Employment - Full Year"),

                // ═══════════════════════════════════════════════════════════════
                // Employee B — Mid-Year Promotion
                // Rs. 150,000 × 6 months + Rs. 250,000 × 6 months = Rs. 2,400,000 actual
                // WITHOUT our system: employer assumes Rs. 250,000 × 12 = Rs. 3,000,000 → overtaxed
                // WITH our system: uses actual cumulative → correct tax on Rs. 2,400,000
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000002", "Bhagya Silva", "850200002V",
                    "bhagya.silva@test.com", "+94771000002",
                    new DateTime(2025, 4, 1), null,
                    new[] {
                        (4, 150_000m), (5, 150_000m), (6, 150_000m), (7, 150_000m),
                        (8, 150_000m), (9, 150_000m),
                        (10, 250_000m), (11, 250_000m), (12, 250_000m),
                        (1, 250_000m), (2, 250_000m), (3, 250_000m)
                    },
                    0m, 0m, "Mid-Year Promotion"),

                // ═══════════════════════════════════════════════════════════════
                // Employee C — Job Change (Company X → Company Y)
                // Company X: Rs. 300,000 × 4 months (Apr-Jul) = Rs. 1,200,000
                // Company Y: Rs. 400,000 × 8 months (Aug-Mar) = Rs. 3,200,000
                // Actual annual: Rs. 4,400,000
                // WITHOUT system: Company Y assumes Rs. 400,000 × 12 = Rs. 4,800,000 → overtaxed
                // WITH system: IRD data shows Rs. 1,200,000 income + tax already paid → correct
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000003", "Chaminda Fernando", "880300003V",
                    "chaminda.fernando@test.com", "+94771000003",
                    new DateTime(2025, 8, 1), null,
                    new[] {
                        (8, 400_000m), (9, 400_000m), (10, 400_000m), (11, 400_000m),
                        (12, 400_000m), (1, 400_000m), (2, 400_000m), (3, 400_000m)
                    },
                    // Prior employer: 4 months × Rs. 300,000 = Rs. 1,200,000 income
                    // Prior tax: on Rs. 3,600,000 projected (300k×12), monthly = Rs. 18,500, paid 4 months
                    1_200_000m,
                    PayeCalculator.CalculateMonthlyDeduction(300_000m * 12) * 4,
                    "Job Change - Prior Employer IRD Data"),

                // ═══════════════════════════════════════════════════════════════
                // Employee D — Layoff & Re-employment
                // Rs. 250,000 × 5 months (Apr-Aug) = Rs. 1,250,000
                // Unemployed 4 months (Sep-Dec)
                // Rs. 200,000 × 3 months (Jan-Mar) = Rs. 600,000
                // Actual annual: Rs. 1,850,000
                // WITHOUT system: new employer assumes Rs. 200,000 × 12 = Rs. 2,400,000 → wrong
                // WITH system: IRD shows Rs. 1,250,000 + tax paid → adjusted for 3 months
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000004", "Dilini Jayasinghe", "920400004V",
                    "dilini.jayasinghe@test.com", "+94771000004",
                    new DateTime(2026, 1, 1), null,
                    new[] {
                        (1, 200_000m), (2, 200_000m), (3, 200_000m)
                    },
                    // Prior employer: 5 months × Rs. 250,000 = Rs. 1,250,000
                    // Prior tax: on Rs. 3,000,000 projected (250k×12), monthly = Rs. 8,000, paid 5 months
                    1_250_000m,
                    PayeCalculator.CalculateMonthlyDeduction(250_000m * 12) * 5,
                    "Layoff & Re-employment"),

                // ═══════════════════════════════════════════════════════════════
                // Employee E — Migration Attempt
                // Rs. 350,000 × 3 months (Apr-Jun) at old job = Rs. 1,050,000
                // Resigned Jul, migration failed
                // Unemployed Jul-Sep (3 months)
                // Rs. 350,000 × 6 months (Oct-Mar) at new job = Rs. 2,100,000
                // Actual annual: Rs. 3,150,000
                // WITHOUT system: new employer assumes Rs. 350,000 × 12 = Rs. 4,200,000 → overtaxed
                // WITH system: IRD shows Rs. 1,050,000 + tax paid → correct adjusted deduction
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000005", "Eranga Bandara", "870500005V",
                    "eranga.bandara@test.com", "+94771000005",
                    new DateTime(2025, 10, 1), null,
                    new[] {
                        (10, 350_000m), (11, 350_000m), (12, 350_000m),
                        (1, 350_000m), (2, 350_000m), (3, 350_000m)
                    },
                    // Prior employer: 3 months × Rs. 350,000 = Rs. 1,050,000
                    // Prior tax: on Rs. 4,200,000 projected (350k×12), monthly = Rs. 26,500, paid 3 months
                    1_050_000m,
                    PayeCalculator.CalculateMonthlyDeduction(350_000m * 12) * 3,
                    "Migration Attempt - Re-employed"),

                // ═══════════════════════════════════════════════════════════════
                // Employee F — Below Tax Relief (no tax)
                // Rs. 120,000 × 12 = Rs. 1,440,000 < Rs. 1,800,000 relief
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000006", "Fathima Rizna", "950600006V",
                    "fathima.rizna@test.com", "+94771000006",
                    new DateTime(2025, 4, 1), null,
                    new[] {
                        (4, 120_000m), (5, 120_000m), (6, 120_000m), (7, 120_000m),
                        (8, 120_000m), (9, 120_000m), (10, 120_000m), (11, 120_000m),
                        (12, 120_000m), (1, 120_000m), (2, 120_000m), (3, 120_000m)
                    },
                    0m, 0m, "Below Tax Relief - No Tax"),

                // ═══════════════════════════════════════════════════════════════
                // Employee G — High Earner (36% slab)
                // Rs. 600,000 × 12 = Rs. 7,200,000 annual
                // Annual tax: Rs. 1,464,000 | Monthly: Rs. 122,000
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000007", "Gayan Wickrama", "800700007V",
                    "gayan.wickrama@test.com", "+94771000007",
                    new DateTime(2025, 4, 1), null,
                    new[] {
                        (4, 600_000m), (5, 600_000m), (6, 600_000m), (7, 600_000m),
                        (8, 600_000m), (9, 600_000m), (10, 600_000m), (11, 600_000m),
                        (12, 600_000m), (1, 600_000m), (2, 600_000m), (3, 600_000m)
                    },
                    0m, 0m, "High Earner - 36% Slab"),

                // ═══════════════════════════════════════════════════════════════
                // Employee H — Resigned Mid-Year
                // Rs. 180,000 × 9 months (Apr-Dec) = Rs. 1,620,000
                // Below relief → Rs. 0 tax
                // ═══════════════════════════════════════════════════════════════
                new EmployeeScenario("100000008", "Harsha Rathnayake", "860800008V",
                    "harsha.rathnayake@test.com", "+94771000008",
                    new DateTime(2025, 4, 1), new DateTime(2025, 12, 31),
                    new[] {
                        (4, 180_000m), (5, 180_000m), (6, 180_000m), (7, 180_000m),
                        (8, 180_000m), (9, 180_000m), (10, 180_000m), (11, 180_000m),
                        (12, 180_000m)
                    },
                    0m, 0m, "Resigned Mid-Year"),
            };

            foreach (var s in scenarios)
            {
                var emp = new Employee
                {
                    Id = Guid.NewGuid(), TIN = s.TIN, FullName = s.Name,
                    NICNumber = s.NIC, ContactEmail = s.Email,
                    ContactPhone = s.Phone, CreatedAt = DateTime.UtcNow
                };
                db.Employees.Add(emp);

                db.AppUsers.Add(new AppUser
                {
                    Email = s.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(PWD),
                    Role = "Employee", FullName = s.Name, TIN = s.TIN,
                    IsActive = true, CreatedAt = DateTime.UtcNow
                });

                var payroll = new EmployeePayroll
                {
                    Id = Guid.NewGuid(), EmployerId = companyY.Id, EmployeeId = emp.Id,
                    GrossMonthlySalary = s.MonthlySalaries.Last().salary,
                    EmploymentStartDate = s.StartDate,
                    EmploymentEndDate = s.EndDate,
                    EffectiveDate = s.StartDate,
                    IsActive = s.EndDate == null || s.EndDate > DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                db.EmployeePayrolls.Add(payroll);

                // Store IRD cumulative data if employee has prior employer
                if (s.PriorIncome > 0 || s.PriorDeduction > 0)
                {
                    db.IrdCumulativeCaches.Add(new IrdCumulativeCache
                    {
                        EmployeeTIN = s.TIN, FinancialYear = FY,
                        CumulativeIncome = s.PriorIncome,
                        CumulativeDeduction = s.PriorDeduction,
                        RetrievedAt = DateTime.UtcNow,
                        RetrievedByEmployerId = companyY.Id
                    });
                }

                // ── Generate monthly deductions using ACTUAL cumulative method ──
                decimal cumIncome = s.PriorIncome;
                decimal cumDeduction = s.PriorDeduction;
                int totalMonthsInFY = 12;

                for (int i = 0; i < s.MonthlySalaries.Length; i++)
                {
                    var (month, salary) = s.MonthlySalaries[i];
                    int year = month >= 4 ? 2025 : 2026;
                    int remainingMonths = s.MonthlySalaries.Length - i;

                    // Project annual income: actual income so far + remaining months at current salary
                    decimal projectedAnnual = cumIncome + salary + (salary * (remainingMonths - 1));
                    decimal annualTax = PayeCalculator.CalculateAnnualTax(projectedAnnual);

                    // Adjusted monthly: (annual tax - already paid) / remaining months
                    decimal monthlyDeduction = PayeCalculator.CalculateAdjustedDeduction(
                        projectedAnnual, cumDeduction, remainingMonths);

                    db.MonthlyDeductions.Add(new MonthlyDeduction
                    {
                        Id = Guid.NewGuid(),
                        EmployeePayrollId = payroll.Id,
                        Month = month, Year = year,
                        GrossIncome = salary,
                        AnnualTaxLiability = annualTax,
                        MonthlyDeductionAmount = monthlyDeduction,
                        CumulativeDeductionAtCalculation = cumDeduction,
                        RemainingMonthsAtCalculation = remainingMonths,
                        CalculationTrigger = i == 0 ? "InitialEntry" : "Recalculation",
                        IsOverpaid = false,
                        IsLocked = true,
                        CalculatedAt = new DateTime(year, month, 15)
                    });

                    cumIncome += salary;
                    cumDeduction += monthlyDeduction;
                }
            }

            db.SaveChanges();
            Console.WriteLine("[SeedData] Successfully seeded 8 employees with realistic PAYE scenarios.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedData] Error: {ex.Message}");
        }
    }

    private record EmployeeScenario(
        string TIN, string Name, string NIC, string Email, string Phone,
        DateTime StartDate, DateTime? EndDate,
        (int month, decimal salary)[] MonthlySalaries,
        decimal PriorIncome, decimal PriorDeduction,
        string Scenario);
}
