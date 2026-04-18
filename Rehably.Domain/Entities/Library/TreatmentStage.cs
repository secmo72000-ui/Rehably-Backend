using Rehably.Domain.Entities.Base;

namespace Rehably.Domain.Entities.Library;

public class TreatmentStage : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? BodyRegionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Description { get; set; }
    public int? MinWeeks { get; set; }
    public int? MaxWeeks { get; set; }
    public int? MinSessions { get; set; }
    public int? MaxSessions { get; set; }

    public virtual BodyRegionCategory? BodyRegion { get; set; }
}
