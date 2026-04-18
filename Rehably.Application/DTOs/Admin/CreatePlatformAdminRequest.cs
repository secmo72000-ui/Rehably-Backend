using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Admin;

public record CreatePlatformAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    public string LastName { get; init; } = string.Empty;

    [Required]
    public string RoleId { get; init; } = string.Empty;  // Must assign a role
}
