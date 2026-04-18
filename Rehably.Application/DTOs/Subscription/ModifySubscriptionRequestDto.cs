namespace Rehably.Application.DTOs.Subscription;

public record ModifySubscriptionRequestDto
{
    public List<Guid> AddFeatureIds { get; init; } = new();
    public List<Guid> RemoveFeatureIds { get; init; } = new();
    public List<FeatureQuantityUpdateDto> UpdateQuantities { get; init; } = new();
}
