namespace Rehably.Infrastructure.Services.Platform;

public static class ProrationCalculator
{
    public static ProrationResult Calculate(
        decimal oldPrice,
        decimal newPrice,
        DateOnly cycleStart,
        DateOnly cycleEnd,
        DateOnly changeDate)
    {
        var daysInCycle = cycleEnd.DayNumber - cycleStart.DayNumber;
        if (daysInCycle <= 0)
            throw new ArgumentException("Invalid cycle dates: cycleEnd must be after cycleStart.");

        var daysRemaining = cycleEnd.DayNumber - changeDate.DayNumber;
        if (daysRemaining <= 0)
            return new ProrationResult(0m, 0m, 0m, daysInCycle, 0);

        var unusedCredit  = Math.Round(oldPrice * daysRemaining / daysInCycle, 2, MidpointRounding.AwayFromZero);
        var newPlanCharge = Math.Round(newPrice * daysRemaining / daysInCycle, 2, MidpointRounding.AwayFromZero);
        var amountDue     = newPlanCharge - unusedCredit;

        return new ProrationResult(unusedCredit, newPlanCharge, amountDue, daysInCycle, daysRemaining);
    }
}

public record ProrationResult(
    decimal UnusedCredit,
    decimal NewPlanCharge,
    decimal AmountDue,
    int DaysInCycle,
    int DaysRemaining);
