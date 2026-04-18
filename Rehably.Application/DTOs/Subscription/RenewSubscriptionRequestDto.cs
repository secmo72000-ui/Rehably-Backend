namespace Rehably.Application.DTOs.Subscription;

public record RenewSubscriptionRequestDto
{
    public Guid PackageId { get; init; }
}
