namespace Rehably.Application.DTOs.Auth;

public record OtpRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty; // "login" | "password_reset"
}
