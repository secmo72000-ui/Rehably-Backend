namespace Rehably.Application.DTOs.Auth;

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive { get; init; }
    public bool MustChangePassword { get; init; }
    public bool EmailVerified { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? ClinicId { get; init; }
    public List<string> Roles { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public int AccessFailedCount { get; init; }
    public DateTime? LockoutEnd { get; init; }
}
