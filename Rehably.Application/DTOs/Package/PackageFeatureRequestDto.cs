namespace Rehably.Application.DTOs.Package;

public record PackageFeatureRequestDto
{
    public Guid FeatureId { get; init; }
    public int? Limit { get; init; }
    public decimal? CalculatedPrice { get; init; }
    public bool IsIncluded { get; init; } = true;
}
