namespace Rehably.Domain.Entities.Platform;

public class UsageAuditLog
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? FeatureCode { get; set; }
    public int? Limit { get; set; }
    public int? Used { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
}
