namespace Rehably.Domain.Entities.Audit;

public class AuditLog
{
    public Guid Id { get; set; }
    public string? TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? OtpReference { get; set; }

    public ICollection<AuditLogEntry> Entries { get; set; } = new List<AuditLogEntry>();
}
