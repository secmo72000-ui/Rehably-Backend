using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Audit;

public record AuditLogQueryDto
{
    /// <summary>Filter by clinic ID</summary>
    public Guid? ClinicId { get; init; }

    /// <summary>Filter by user ID</summary>
    public Guid? UserId { get; init; }

    /// <summary>Filter by action type</summary>
    public AuditActionType? ActionType { get; init; }

    /// <summary>Filter by user role</summary>
    public string? Role { get; init; }

    /// <summary>Filter by user email</summary>
    public string? Email { get; init; }

    /// <summary>Filter by success status</summary>
    public bool? IsSuccess { get; init; }

    /// <summary>Filter from date</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>Filter to date</summary>
    public DateTime? EndDate { get; init; }

    /// <summary>Page number (default: 1)</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size (default: 20)</summary>
    public int PageSize { get; init; } = 20;
}
