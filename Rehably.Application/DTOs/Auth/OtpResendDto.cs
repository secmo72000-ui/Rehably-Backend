namespace Rehably.Application.DTOs.Auth;

public record OtpResendDto
{
    public string Email { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
}
