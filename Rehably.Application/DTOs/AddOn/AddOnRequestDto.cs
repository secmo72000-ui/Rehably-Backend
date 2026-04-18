namespace Rehably.Application.DTOs.AddOn;

public record AddOnRequestDto
{
    /// <summary>The feature being requested as an add-on.</summary>
    public Guid FeatureId { get; init; }

    /// <summary>Optional notes for the admin reviewing this request.</summary>
    public string? Notes { get; init; }
}
