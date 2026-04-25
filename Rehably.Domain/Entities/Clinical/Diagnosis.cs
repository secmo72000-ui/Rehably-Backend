using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Library;

namespace Rehably.Domain.Entities.Clinical;

/// <summary>
/// A clinical diagnosis entry.
/// Global entries (ClinicId = null) are seeded from ICD-10 by super-admin.
/// Clinic-specific entries (ClinicId != null) are added by the clinic manager.
/// </summary>
public class Diagnosis : BaseEntity
{
    /// <summary>Null = global (ICD-10). Non-null = clinic-custom.</summary>
    public Guid? ClinicId { get; set; }

    public Guid SpecialityId { get; set; }

    /// <summary>Optional link to a body region (e.g. Knee, Shoulder).</summary>
    public Guid? BodyRegionCategoryId { get; set; }

    // ── ICD-10 fields ──────────────────────────────────────────────────────────
    public string IcdCode { get; set; } = string.Empty;       // e.g. M54.5
    public string NameEn { get; set; } = string.Empty;        // Low back pain
    public string NameAr { get; set; } = string.Empty;        // ألم أسفل الظهر

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // ── Auto-suggest protocol ──────────────────────────────────────────────────
    /// <summary>Suggested treatment protocol name shown when this diagnosis is picked.</summary>
    public string? DefaultProtocolName { get; set; }

    /// <summary>JSON array of Exercise IDs to auto-suggest.</summary>
    public string? DefaultExerciseIds { get; set; }

    /// <summary>Suggested number of sessions.</summary>
    public int? SuggestedSessions { get; set; }

    /// <summary>Suggested duration in weeks.</summary>
    public int? SuggestedDurationWeeks { get; set; }

    // Navigation
    public Speciality Speciality { get; set; } = null!;
    public BodyRegionCategory? BodyRegion { get; set; }
}
