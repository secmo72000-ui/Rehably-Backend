using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Exercise in the global library.
/// </summary>
public class Exercise : BaseEntity
{
    public Guid? ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public Guid BodyRegionCategoryId { get; set; }
    public string? RelatedConditionCode { get; set; }
    public string? Tags { get; set; }
    public int? Repeats { get; set; }
    public int? Steps { get; set; }
    public int? HoldSeconds { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? LinkedExerciseIds { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;

    public virtual Clinic? Clinic { get; set; }
    public virtual BodyRegionCategory BodyRegionCategory { get; set; } = null!;
}
