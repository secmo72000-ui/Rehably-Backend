namespace Rehably.Application.DTOs.Admin;

public record CreatePlatformRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Permissions { get; init; } = new();
}
