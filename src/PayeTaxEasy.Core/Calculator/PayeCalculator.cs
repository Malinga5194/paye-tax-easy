namespace PayeTaxEasy.Core.Calculator;

/// <summary>
/// Pure, stateless PAYE tax calculator based on the Inland Revenue Amendment Act No. 02 of 2025.
/// Financial year: 1 April – 31 March (Sri Lanka).
/// </summary>
public static class PayeCalculator
{
    // Tax slab boundaries
    private const decimal TaxRelief = 1_800_000m;
    private const decimal Slab1Max = 1_000_000m;  // 6%  band width
    private const decimal Slab2Max = 500_000m;     // 18% band width
    private const decimal Slab3Max = 500_000m;     // 24% band width
    private const decimal Slab4Max = 500_000m;     // 30% band width

    private const decimal Rate1 = 0.06m;
    private const decimal Rate2 = 0.18m;
    private const decimal Rate3 = 0.24m;
    private const decimal Rate4 = 0.30m;
    private const decimal Rate5 = 0.36m;

    /// <summary>
    /// Computes the annual PAYE tax liability for a given annual income using the progressive slab formula.
    /// Returns 0 for non-positive income.
    /// </summary>
    public static decimal CalculateAnnualTax(decimal annualIncome)
    {
        if (annualIncome <= 0m) return 0m;

        decimal tax = 0m;
        decimal taxableIncome = Math.Max(0m, annualIncome - TaxRelief);

        // Slab 1: 6% on up to Rs. 1,000,000 above relief
        decimal slab1 = Math.Min(taxableIncome, Slab1Max);
        tax += slab1 * Rate1;
        taxableIncome -= slab1;
        if (taxableIncome <= 0m) return tax;

        // Slab 2: 18% on next Rs. 500,000
        decimal slab2 = Math.Min(taxableIncome, Slab2Max);
        tax += slab2 * Rate2;
        taxableIncome -= slab2;
        if (taxableIncome <= 0m) return tax;

        // Slab 3: 24% on next Rs. 500,000
        decimal slab3 = Math.Min(taxableIncome, Slab3Max);
        tax += slab3 * Rate3;
        taxableIncome -= slab3;
        if (taxableIncome <= 0m) return tax;

        // Slab 4: 30% on next Rs. 500,000
        decimal slab4 = Math.Min(taxableIncome, Slab4Max);
        tax += slab4 * Rate4;
        taxableIncome -= slab4;
        if (taxableIncome <= 0m) return tax;

        // Slab 5: 36% on remainder
        tax += taxableIncome * Rate5;

        return tax;
    }

    /// <summary>
    /// Computes the standard monthly PAYE deduction for a given annual income.
    /// Result is rounded to the nearest Sri Lankan Rupee.
    /// </summary>
    public static decimal CalculateMonthlyDeduction(decimal annualIncome)
    {
        decimal annualTax = CalculateAnnualTax(annualIncome);
        return Math.Round(annualTax / 12m, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Computes the adjusted monthly deduction for an employee who has prior cumulative deductions
    /// from earlier in the financial year (e.g., from a previous employer).
    /// Formula: Max(0, Round((AnnualTax(projectedAnnualIncome) - cumulativeDeduction) / remainingMonths, 0))
    /// </summary>
    /// <param name="projectedAnnualIncome">Total projected annual income for the financial year.</param>
    /// <param name="cumulativeDeduction">PAYE already deducted this financial year across all employers.</param>
    /// <param name="remainingMonths">Months remaining in the financial year (1–12 inclusive).</param>
    public static decimal CalculateAdjustedDeduction(
        decimal projectedAnnualIncome,
        decimal cumulativeDeduction,
        int remainingMonths)
    {
        if (remainingMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(remainingMonths), "Remaining months must be at least 1.");

        decimal annualTax = CalculateAnnualTax(projectedAnnualIncome);
        decimal remaining = annualTax - cumulativeDeduction;
        if (remaining <= 0m) return 0m;

        return Math.Round(remaining / remainingMonths, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Computes the adjusted monthly deduction when the employee has zero-income months (employment gaps).
    /// Zero-income months are excluded from the denominator so the remaining tax is spread only across
    /// active employment months.
    /// </summary>
    /// <param name="projectedAnnualIncome">Total projected annual income (excluding zero-income months).</param>
    /// <param name="cumulativeDeduction">PAYE already deducted this financial year.</param>
    /// <param name="remainingMonths">Total calendar months remaining in the financial year.</param>
    /// <param name="zeroIncomeMonths">Number of months within remainingMonths with zero income.</param>
    public static decimal CalculateWithGap(
        decimal projectedAnnualIncome,
        decimal cumulativeDeduction,
        int remainingMonths,
        int zeroIncomeMonths)
    {
        if (remainingMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(remainingMonths), "Remaining months must be at least 1.");
        if (zeroIncomeMonths < 0 || zeroIncomeMonths >= remainingMonths)
            throw new ArgumentOutOfRangeException(nameof(zeroIncomeMonths),
                "Zero-income months must be non-negative and less than remaining months.");

        int activeMonths = remainingMonths - zeroIncomeMonths;
        if (activeMonths <= 0) return 0m;

        decimal annualTax = CalculateAnnualTax(projectedAnnualIncome);
        decimal remaining = annualTax - cumulativeDeduction;
        if (remaining <= 0m) return 0m;

        return Math.Round(remaining / activeMonths, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Computes the proportional monthly deduction for one employer when an employee works for
    /// two employers simultaneously.
    /// Formula: AdjustedMonthlyTotal × (employerGross / totalGross)
    /// </summary>
    /// <param name="employerGross">This employer's monthly gross salary contribution.</param>
    /// <param name="totalGross">Combined monthly gross salary from all employers.</param>
    /// <param name="adjustedMonthlyTotal">The total adjusted monthly deduction across all employers.</param>
    public static decimal CalculateDualEmployerShare(
        decimal employerGross,
        decimal totalGross,
        decimal adjustedMonthlyTotal)
    {
        if (totalGross <= 0m)
            throw new ArgumentOutOfRangeException(nameof(totalGross), "Total gross must be positive.");
        if (employerGross < 0m)
            throw new ArgumentOutOfRangeException(nameof(employerGross), "Employer gross cannot be negative.");
        if (employerGross > totalGross)
            throw new ArgumentOutOfRangeException(nameof(employerGross),
                "Employer gross cannot exceed total gross.");

        decimal share = employerGross / totalGross;
        return Math.Round(adjustedMonthlyTotal * share, 0, MidpointRounding.AwayFromZero);
    }
}
