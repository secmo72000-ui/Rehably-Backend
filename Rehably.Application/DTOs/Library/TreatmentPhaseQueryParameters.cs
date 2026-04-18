namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Query parameters specific to treatment phase filtering.
/// </summary>
public record TreatmentPhaseQueryParameters : LibraryQueryParameters
{
    public string? TreatmentCode { get; init; }
}
