using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Library;

/// <summary>
/// Lookup table for body region categories used in treatments and exercises.
/// </summary>
public class BodyRegionCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameArabic { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
