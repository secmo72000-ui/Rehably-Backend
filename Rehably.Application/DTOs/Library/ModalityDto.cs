using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Library;

/// <summary>
/// DTO for therapeutic modality in the global library.
/// </summary>
public record ModalityDto
{
    public Guid Id { get; set; }
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public ModalityType ModalityType { get; set; }
    public string TherapeuticCategory { get; set; } = string.Empty;
    public string MainGoal { get; set; } = string.Empty;
    public string? ParametersNotes { get; set; }
    public string? ClinicalNotes { get; set; }
    public int? MinDurationWeeks { get; set; }
    public int? MaxDurationWeeks { get; set; }
    public int? MinSessions { get; set; }
    public int? MaxSessions { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public LibraryAccessTier AccessTier { get; set; }

    /// <summary>
    /// Computed property: true if ClinicId is null (global item).
    /// </summary>
    public bool IsGlobal => ClinicId == null;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
