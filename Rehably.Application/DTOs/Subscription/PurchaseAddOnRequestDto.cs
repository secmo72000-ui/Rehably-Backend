namespace Rehably.Application.DTOs.Subscription;

public record PurchaseAddOnRequestDto
{
    public Guid FeatureId { get; init; }
    public int Quantity { get; init; } = 1;
}
