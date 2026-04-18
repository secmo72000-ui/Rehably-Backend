namespace Rehably.Application.DTOs.Auth;

public record RequestOtpLoginDto
{
    public string Email { get; init; } = string.Empty;
}
