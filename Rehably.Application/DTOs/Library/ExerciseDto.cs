using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for exercise in the global library.
/// </summary>
public record ExerciseDto
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public Guid BodyRegionCategoryId { get; set; }
    public string? BodyRegionCategoryName { get; set; }
    public string? RelatedConditionCode { get; set; }
    public string? Tags { get; set; }
    public int? Repeats { get; set; }
    public int? Steps { get; set; }
    public int? HoldSeconds { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? LinkedExerciseIds { get; set; }
    public LibraryAccessTier AccessTier { get; set; }

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
