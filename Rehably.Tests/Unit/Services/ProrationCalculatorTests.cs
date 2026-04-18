using FluentAssertions;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class ProrationCalculatorTests
{
    [Fact]
    public void Calculate_MidCycleUpgrade_ReturnsCorrectProration()
    {
        // Day 15 of a 30-day cycle: $100 -> $200
        // 15 days remaining out of 30
        // UnusedCredit  = 100 * 15/30 = 50.00
        // NewPlanCharge = 200 * 15/30 = 100.00
        // AmountDue     = 100 - 50   = 50.00
        var cycleStart = new DateOnly(2026, 3, 1);
        var cycleEnd   = new DateOnly(2026, 3, 31);
        var changeDate = new DateOnly(2026, 3, 16); // 15 days remaining (31-16=15)

        var result = ProrationCalculator.Calculate(100m, 200m, cycleStart, cycleEnd, changeDate);

        result.DaysInCycle.Should().Be(30);
        result.DaysRemaining.Should().Be(15);
        result.UnusedCredit.Should().Be(50.00m);
        result.NewPlanCharge.Should().Be(100.00m);
        result.AmountDue.Should().Be(50.00m);
    }

    [Fact]
    public void Calculate_FirstDayOfCycle_FullCharge()
    {
        // changeDate == cycleStart => daysRemaining == daysInCycle
        var cycleStart = new DateOnly(2026, 3, 1);
        var cycleEnd   = new DateOnly(2026, 3, 31);
        var changeDate = new DateOnly(2026, 3, 1);

        var result = ProrationCalculator.Calculate(100m, 200m, cycleStart, cycleEnd, changeDate);

        result.DaysInCycle.Should().Be(30);
        result.DaysRemaining.Should().Be(30);
        result.UnusedCredit.Should().Be(100.00m);
        result.NewPlanCharge.Should().Be(200.00m);
        result.AmountDue.Should().Be(100.00m);
    }

    [Fact]
    public void Calculate_LastDayOfCycle_ReturnsZeroDue()
    {
        // changeDate == cycleEnd => daysRemaining == 0
        var cycleStart = new DateOnly(2026, 3, 1);
        var cycleEnd   = new DateOnly(2026, 3, 31);
        var changeDate = new DateOnly(2026, 3, 31);

        var result = ProrationCalculator.Calculate(100m, 200m, cycleStart, cycleEnd, changeDate);

        result.DaysRemaining.Should().Be(0);
        result.UnusedCredit.Should().Be(0m);
        result.NewPlanCharge.Should().Be(0m);
        result.AmountDue.Should().Be(0m);
    }

    [Fact]
    public void Calculate_AnnualPlan_ProRatesOver365Days()
    {
        // Annual plan: $1200/year, upgrade after 30 days
        // cycleStart = Jan 1, cycleEnd = Dec 31 => daysInCycle = 364
        // changeDate = Jan 31 => daysRemaining = 364 - 30 = 334
        var cycleStart = new DateOnly(2026, 1, 1);
        var cycleEnd   = new DateOnly(2026, 12, 31);
        var changeDate = new DateOnly(2026, 1, 31); // 30 days into cycle

        var result = ProrationCalculator.Calculate(1200m, 2400m, cycleStart, cycleEnd, changeDate);

        result.DaysInCycle.Should().Be(364);
        result.DaysRemaining.Should().Be(334);
        result.UnusedCredit.Should().Be(Math.Round(1200m * 334m / 364m, 2, MidpointRounding.AwayFromZero));
        result.NewPlanCharge.Should().Be(Math.Round(2400m * 334m / 364m, 2, MidpointRounding.AwayFromZero));
        result.AmountDue.Should().Be(result.NewPlanCharge - result.UnusedCredit);
    }

    [Fact]
    public void Calculate_SamePrice_ReturnsZeroDue()
    {
        var cycleStart = new DateOnly(2026, 3, 1);
        var cycleEnd   = new DateOnly(2026, 3, 31);
        var changeDate = new DateOnly(2026, 3, 16);

        var result = ProrationCalculator.Calculate(100m, 100m, cycleStart, cycleEnd, changeDate);

        result.AmountDue.Should().Be(0m);
        result.UnusedCredit.Should().Be(result.NewPlanCharge);
    }

    [Fact]
    public void Calculate_FebruaryMonth_Uses28Days()
    {
        // Feb 1 - Feb 28 = 27 days in cycle (DateOnly DayNumber difference)
        var cycleStart = new DateOnly(2026, 2, 1);
        var cycleEnd   = new DateOnly(2026, 2, 28);
        var changeDate = new DateOnly(2026, 2, 15); // 13 days remaining

        var result = ProrationCalculator.Calculate(100m, 200m, cycleStart, cycleEnd, changeDate);

        result.DaysInCycle.Should().Be(27);
        result.DaysRemaining.Should().Be(13);
        result.UnusedCredit.Should().Be(Math.Round(100m * 13m / 27m, 2, MidpointRounding.AwayFromZero));
        result.NewPlanCharge.Should().Be(Math.Round(200m * 13m / 27m, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void Calculate_InvalidCycle_ThrowsArgumentException()
    {
        // cycleEnd before cycleStart => daysInCycle <= 0
        var cycleStart = new DateOnly(2026, 3, 31);
        var cycleEnd   = new DateOnly(2026, 3, 1);
        var changeDate = new DateOnly(2026, 3, 15);

        var act = () => ProrationCalculator.Calculate(100m, 200m, cycleStart, cycleEnd, changeDate);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid cycle dates*");
    }
}
