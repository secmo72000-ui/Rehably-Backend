using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for medical device in the global library.
/// </summary>
public record DeviceDto
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public LibraryAccessTier AccessTier { get; set; }

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
