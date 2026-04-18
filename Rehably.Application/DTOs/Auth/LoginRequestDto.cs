namespace Rehably.Application.DTOs.Auth;

public record LoginRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
