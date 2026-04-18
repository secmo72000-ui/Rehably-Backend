using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record CreateUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public RoleType RoleType { get; init; }
}
