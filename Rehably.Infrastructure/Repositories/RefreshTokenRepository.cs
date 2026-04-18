using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IRefreshTokenRepository
/// </summary>
public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetValidTokenWithUserAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<int> CleanupExpiredTokensAsync(string userId)
    {
        var expiredTokens = await _dbSet
            .Where(rt => rt.UserId == userId && (rt.IsRevoked || rt.ExpiresAt <= DateTime.UtcNow))
            .ToListAsync();

        _dbSet.RemoveRange(expiredTokens);
        return expiredTokens.Count;
    }

    public async Task<RefreshToken?> GetValidTokenAsync(string token)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<RefreshToken?> GetValidTokenAsync(string userId, string token)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rt =>
                rt.UserId == userId &&
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RevokeAllForClinicAsync(Guid clinicId)
    {
        var tokens = await _dbSet
            .Include(rt => rt.User)
            .Where(rt => rt.User.ClinicId == clinicId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }
}
