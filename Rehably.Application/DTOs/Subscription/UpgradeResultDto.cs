namespace Rehably.Application.DTOs.Subscription;

public record UpgradeResultDto
{
    public Guid SubscriptionId { get; init; }
    public string OldPackageName { get; init; } = string.Empty;
    public string NewPackageName { get; init; } = string.Empty;
    public decimal ProratedAmount { get; init; }
    public Guid? InvoiceId { get; init; }
}
