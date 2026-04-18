using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for clinic-specific override of a global library item.
/// </summary>
public record ClinicLibraryOverrideDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public LibraryType LibraryType { get; init; }
    public Guid GlobalItemId { get; init; }
    public string? OverrideDataJson { get; init; }
    public bool IsHidden { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
