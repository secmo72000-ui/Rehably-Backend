using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// Medical speciality (e.g. Orthopaedics, Neurology, Paediatrics).
/// Managed globally by super-admin. Each speciality maps to one or more
/// ICD-10 chapters so the diagnosis list can be filtered accordingly.
/// </summary>
public class Speciality : BaseEntity
{
    public string Code { get; set; } = string.Empty;          // e.g. "ORTHO"
    public string NameEn { get; set; } = string.Empty;        // Orthopaedics
    public string NameAr { get; set; } = string.Empty;        // العظام والمفاصل

    /// <summary>
    /// Comma-separated ICD-10 chapter letters/ranges to pre-filter diagnoses.
    /// e.g. "M" for musculoskeletal, "G" for neurological.
    /// </summary>
    public string IcdChapters { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
}
