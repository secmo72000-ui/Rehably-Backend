namespace Rehably.Application.DTOs.Auth;

public record PasswordResetWithTokenDto
{
    public string ResetToken { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
