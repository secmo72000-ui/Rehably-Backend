using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Tenant;

public class UsageHistory
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public MetricType MetricType { get; set; }
    public long Value { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Clinic Clinic { get; set; } = null!;
}
