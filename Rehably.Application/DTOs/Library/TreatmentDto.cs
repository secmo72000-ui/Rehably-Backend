using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for treatment protocol in the global library.
/// </summary>
public record TreatmentDto
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public Guid BodyRegionCategoryId { get; set; }
    public string? BodyRegionCategoryName { get; set; }
    public string AffectedArea { get; set; } = string.Empty;
    public int MinDurationWeeks { get; set; }
    public int MaxDurationWeeks { get; set; }
    public int ExpectedSessions { get; set; }
    public string? Description { get; set; }
    public string? RedFlags { get; set; }
    public string? Contraindications { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? SourceReference { get; set; }
    public string? SourceDetails { get; set; }
    public LibraryAccessTier AccessTier { get; set; }

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
