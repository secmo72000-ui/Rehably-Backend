using Rehably.Domain.Entities.Identity;

namespace Rehably.Application.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetValidTokenWithUserAsync(string token);
    Task RevokeAllForUserAsync(string userId);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(string userId);
    Task<int> CleanupExpiredTokensAsync(string userId);
    Task<RefreshToken?> GetValidTokenAsync(string token);
    Task<RefreshToken?> GetValidTokenAsync(string userId, string token);
    Task RevokeAllForClinicAsync(Guid clinicId);
}
