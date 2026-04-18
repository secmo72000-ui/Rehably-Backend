namespace Rehably.Application.DTOs.Audit;

public record UserAuditInfoDto
{
    /// <summary>User's email address</summary>
    public string? Email { get; init; }

    /// <summary>User's primary role name</summary>
    public string? RoleName { get; init; }
}
