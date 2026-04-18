namespace Rehably.Application.Services.Auth;

public interface ITokenService
{
    string GenerateAccessToken(string userId, Guid? tenantId, Guid? clinicId, IList<string> roles, bool mustChangePassword = false);
    Task<string> GenerateAccessTokenAsync(string userId, Guid? tenantId, Guid? clinicId, IList<string> roles, IEnumerable<string> permissions, bool mustChangePassword = false);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task<HashSet<string>> GetPermissionsForRolesAsync(IList<string> roleNames);
    Task InvalidateClinicTokensAsync(Guid clinicId);
}
