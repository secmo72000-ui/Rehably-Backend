namespace Rehably.Application.DTOs.Subscription;

public record UpgradeSubscriptionRequestDto
{
    public Guid NewPackageId { get; init; }
}
