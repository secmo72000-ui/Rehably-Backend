namespace Rehably.Application.DTOs.Auth;

public record LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool MustChangePassword { get; init; }
    public bool EmailVerified { get; init; }
}
