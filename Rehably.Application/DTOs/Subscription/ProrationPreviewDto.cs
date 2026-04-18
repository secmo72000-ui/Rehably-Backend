namespace Rehably.Application.DTOs.Subscription;

public record ProrationPreviewDto
{
    public string OldPackageName { get; init; } = string.Empty;
    public string NewPackageName { get; init; } = string.Empty;
    public decimal UnusedCredit { get; init; }
    public decimal NewPlanCharge { get; init; }
    public decimal AmountDue { get; init; }
    public int DaysRemaining { get; init; }
    public int DaysInCycle { get; init; }
}
