using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// Request DTO for creating a new treatment protocol.
/// </summary>
public record CreateTreatmentRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? NameArabic { get; init; }
    public Guid BodyRegionCategoryId { get; init; }
    public string AffectedArea { get; init; } = string.Empty;
    public int MinDurationWeeks { get; init; }
    public int MaxDurationWeeks { get; init; }
    public int ExpectedSessions { get; init; }
    public string? Description { get; init; }
    public string? RedFlags { get; init; }
    public string? Contraindications { get; init; }
    public string? ClinicalNotes { get; init; }
    public string? SourceReference { get; init; }
    public string? SourceDetails { get; init; }
    public LibraryAccessTier AccessTier { get; init; } = LibraryAccessTier.Free;
}
