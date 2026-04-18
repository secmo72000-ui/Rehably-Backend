namespace Rehably.Application.DTOs.Subscription;

public record CancelSubscriptionRequestDto
{
    public string? Reason { get; init; }
}
