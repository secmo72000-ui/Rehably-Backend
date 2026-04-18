namespace Rehably.Application.DTOs.Auth;

public record ForgotPasswordRequestDto
{
    public required string Email { get; init; }
}
