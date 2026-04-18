using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Assessment tool in the global library.
/// </summary>
public class Assessment : BaseEntity
{
    public Guid? ClinicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public AssessmentTimePoint TimePoint { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ScoringGuide { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;

    public virtual Clinic? Clinic { get; set; }
}
