namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Query parameters specific to treatment filtering.
/// </summary>
public record TreatmentQueryParameters : LibraryQueryParameters
{
    public string? AffectedArea { get; init; }
}
