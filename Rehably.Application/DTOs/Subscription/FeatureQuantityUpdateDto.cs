namespace Rehably.Application.DTOs.Subscription;

public record FeatureQuantityUpdateDto
{
    public Guid FeatureId { get; init; }
    public int NewQuantity { get; init; }
}
