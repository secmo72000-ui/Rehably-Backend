using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Clinic-specific override for a global library item.
/// Allows clinics to customize global items or hide them.
/// </summary>
public class ClinicLibraryOverride
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public LibraryType LibraryType { get; set; }
    public Guid GlobalItemId { get; set; }
    public string? OverrideDataJson { get; set; }
    public bool IsHidden { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual Clinic Clinic { get; set; } = null!;
}
