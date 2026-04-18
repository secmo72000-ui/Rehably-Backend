namespace Rehably.Application.DTOs.Subscription;

public record SubscriptionModificationResultDto
{
    public SubscriptionDetailDto Subscription { get; init; } = null!;
    public decimal PreviousPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceDifference => NewPrice - PreviousPrice;
    public List<string> ChangesApplied { get; init; } = new();
}
