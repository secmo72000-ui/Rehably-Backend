namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for updating a clinic library override.
/// </summary>
public record UpdateClinicLibraryOverrideRequest
{
    public string? OverrideDataJson { get; init; }
    public bool IsHidden { get; init; }
}
