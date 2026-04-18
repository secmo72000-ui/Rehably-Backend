using System.Text.Json.Serialization;

namespace Rehably.Application.DTOs.Admin;

public record PlatformAdminResponse
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    /// <summary>Only present in the create response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemporaryPassword { get; init; }

    public PlatformRoleResponse Role { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
