using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Tenant;

public class UsageRecord
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public UsageMetric Metric { get; set; }
    public long Value { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime Period { get; set; }

    public Clinic? Clinic { get; set; }
}
