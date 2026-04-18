namespace Rehably.Application.DTOs.Admin;

public record PlatformRoleResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Permissions { get; init; } = new();
    public int UserCount { get; init; }
    public List<RoleUserDto> Users { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
