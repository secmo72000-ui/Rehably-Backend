namespace Rehably.Application.DTOs.Auth;

public record RefreshTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
}