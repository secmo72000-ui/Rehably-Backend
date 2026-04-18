namespace Rehably.Domain.Entities.Audit;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid AuditLogId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public AuditLog AuditLog { get; set; } = null!;
}
