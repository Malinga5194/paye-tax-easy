using PayeTaxEasy.Core.Calculator;
using Xunit;

namespace PayeTaxEasy.Tests.Calculator;

/// <summary>
/// Example-based unit tests for PayeCalculator.
/// Property-based tests (P1–P6) are implemented in separate PBT task files.
/// </summary>
public class PayeCalculatorTests
{
    // ── CalculateAnnualTax ────────────────────────────────────────────────────

    [Fact]
    public void CalculateAnnualTax_ZeroIncome_ReturnsZero()
    {
        Assert.Equal(0m, PayeCalculator.CalculateAnnualTax(0m));
    }

    [Fact]
    public void CalculateAnnualTax_NegativeIncome_ReturnsZero()
    {
        Assert.Equal(0m, PayeCalculator.CalculateAnnualTax(-100_000m));
    }

    [Fact]
    public void CalculateAnnualTax_BelowRelief_ReturnsZero()
    {
        // Rs. 1,800,000 is fully within the tax-relief band → 0 tax
        Assert.Equal(0m, PayeCalculator.CalculateAnnualTax(1_800_000m));
    }

    [Fact]
    public void CalculateAnnualTax_AtSlab1Ceiling_Returns60000()
    {
        // Rs. 2,800,000 → 1,000,000 × 6% = Rs. 60,000
        Assert.Equal(60_000m, PayeCalculator.CalculateAnnualTax(2_800_000m));
    }

    [Fact]
    public void CalculateAnnualTax_AtSlab2Ceiling_Returns150000()
    {
        // Rs. 3,300,000 → 60,000 + 500,000 × 18% = Rs. 150,000
        Assert.Equal(150_000m, PayeCalculator.CalculateAnnualTax(3_300_000m));
    }

    [Fact]
    public void CalculateAnnualTax_AtSlab3Ceiling_Returns270000()
    {
        // Rs. 3,800,000 → 150,000 + 500,000 × 24% = Rs. 270,000
        Assert.Equal(270_000m, PayeCalculator.CalculateAnnualTax(3_800_000m));
    }

    [Fact]
    public void CalculateAnnualTax_AtSlab4Ceiling_Returns420000()
    {
        // Rs. 4,300,000 → 270,000 + 500,000 × 30% = Rs. 420,000
        Assert.Equal(420_000m, PayeCalculator.CalculateAnnualTax(4_300_000m));
    }

    [Fact]
    public void CalculateAnnualTax_AboveSlab4_IncludesTopRate()
    {
        // Rs. 4,800,000 → 420,000 + 500,000 × 36% = Rs. 600,000
        Assert.Equal(600_000m, PayeCalculator.CalculateAnnualTax(4_800_000m));
    }

    // ── CalculateMonthlyDeduction ─────────────────────────────────────────────

    [Fact]
    public void CalculateMonthlyDeduction_ZeroIncome_ReturnsZero()
    {
        Assert.Equal(0m, PayeCalculator.CalculateMonthlyDeduction(0m));
    }

    [Fact]
    public void CalculateMonthlyDeduction_Slab1Ceiling_Returns5000()
    {
        // Annual tax = 60,000 / 12 = 5,000
        Assert.Equal(5_000m, PayeCalculator.CalculateMonthlyDeduction(2_800_000m));
    }

    // ── CalculateAdjustedDeduction ────────────────────────────────────────────

    [Fact]
    public void CalculateAdjustedDeduction_NoPriorDeduction_EqualsStandardMonthly()
    {
        // No prior deduction, 12 months remaining → same as standard monthly
        decimal income = 2_400_000m;
        decimal expected = PayeCalculator.CalculateMonthlyDeduction(income);
        decimal actual = PayeCalculator.CalculateAdjustedDeduction(income, 0m, 12);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalculateAdjustedDeduction_OverpaidEmployee_ReturnsZero()
    {
        // Cumulative deduction exceeds annual tax → should return 0
        decimal income = 2_400_000m;
        decimal annualTax = PayeCalculator.CalculateAnnualTax(income);
        decimal result = PayeCalculator.CalculateAdjustedDeduction(income, annualTax + 1_000m, 6);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateAdjustedDeduction_InvalidRemainingMonths_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PayeCalculator.CalculateAdjustedDeduction(2_400_000m, 0m, 0));
    }

    // ── CalculateWithGap ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateWithGap_ExcludesZeroIncomeMonths()
    {
        decimal income = 2_400_000m;
        decimal annualTax = PayeCalculator.CalculateAnnualTax(income);
        // 6 remaining months, 2 zero-income → spread over 4 active months
        decimal expected = Math.Round(annualTax / 4m, 0, MidpointRounding.AwayFromZero);
        decimal actual = PayeCalculator.CalculateWithGap(income, 0m, 6, 2);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalculateWithGap_AllZeroIncomeMonths_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PayeCalculator.CalculateWithGap(2_400_000m, 0m, 6, 6));
    }

    // ── CalculateDualEmployerShare ────────────────────────────────────────────

    [Fact]
    public void CalculateDualEmployerShare_EqualSplit_HalvesTotal()
    {
        decimal total = 10_000m;
        decimal share = PayeCalculator.CalculateDualEmployerShare(150_000m, 300_000m, total);
        Assert.Equal(5_000m, share);
    }

    [Fact]
    public void CalculateDualEmployerShare_ZeroTotalGross_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PayeCalculator.CalculateDualEmployerShare(100_000m, 0m, 10_000m));
    }
}
