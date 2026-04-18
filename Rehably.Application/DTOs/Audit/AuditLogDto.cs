namespace Rehably.Application.DTOs.Audit;

public record AuditLogDto
{
    /// <summary>Audit log entry identifier</summary>
    public Guid Id { get; init; }

    /// <summary>When the action occurred (UTC)</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>The type of action performed</summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>Clinic ID associated with the action</summary>
    public Guid? ClinicId { get; init; }

    /// <summary>Clinic display name</summary>
    public string? ClinicName { get; init; }

    /// <summary>Package name active at the time</summary>
    public string? PackageName { get; init; }

    /// <summary>User who performed the action</summary>
    public Guid UserId { get; init; }

    /// <summary>Email address of the acting user</summary>
    public string? UserEmail { get; init; }

    /// <summary>Role of the acting user at time of action</summary>
    public string? UserRole { get; init; }

    /// <summary>Name of the entity affected</summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>ID of the entity affected</summary>
    public Guid EntityId { get; init; }

    /// <summary>IP address of the request</summary>
    public string? IpAddress { get; init; }

    /// <summary>User agent string of the request</summary>
    public string? UserAgent { get; init; }

    /// <summary>Additional context details</summary>
    public string? Details { get; init; }

    /// <summary>Whether the action completed successfully</summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>Masked OTP reference in format ****XXX (last 3 chars of HMAC). Null when no OTP was involved.</summary>
    public string? OtpReference { get; init; }
}
