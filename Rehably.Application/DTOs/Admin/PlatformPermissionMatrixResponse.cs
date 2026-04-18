namespace Rehably.Application.DTOs.Admin;

/// <summary>
/// Response containing the platform permission matrix for role creation/editing UI
/// </summary>
public record PlatformPermissionMatrixResponse
{
    /// <summary>
    /// List of resources with their available actions
    /// </summary>
    public List<PermissionResourceDto> Resources { get; init; } = new();
}
