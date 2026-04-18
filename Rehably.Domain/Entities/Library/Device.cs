using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Medical device in the global library.
/// </summary>
public class Device : BaseEntity
{
    public Guid? ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? RelatedConditionCodes { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public LibraryAccessTier AccessTier { get; set; } = LibraryAccessTier.Free;

    public virtual Clinic? Clinic { get; set; }
}
