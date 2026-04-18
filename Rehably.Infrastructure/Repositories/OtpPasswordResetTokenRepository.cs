using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class OtpPasswordResetTokenRepository : Repository<OtpPasswordResetToken>, IOtpPasswordResetTokenRepository
{
    public OtpPasswordResetTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<OtpPasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task<OtpPasswordResetToken?> GetByTokenHashWithUserAsync(string tokenHash)
    {
        return await _dbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }
}
