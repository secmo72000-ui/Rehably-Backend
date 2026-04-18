using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for creating a clinic library override.
/// </summary>
public record CreateClinicLibraryOverrideRequest
{
    public LibraryType LibraryType { get; init; }
    public Guid GlobalItemId { get; init; }
    public string? OverrideDataJson { get; init; }
    public bool IsHidden { get; init; }
}
