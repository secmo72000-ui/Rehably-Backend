using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class OtpCodeRepository : Repository<OtpCode>, IOtpCodeRepository
{
    public OtpCodeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<OtpCode?> GetLatestUnusedAsync(string contact, OtpPurpose purpose)
    {
        return await _context.OtpCodes
            .Where(o => o.Contact == contact && o.Purpose == purpose && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<OtpCode?> GetLatestAsync(string contact, OtpPurpose purpose)
    {
        return await _context.OtpCodes
            .Where(o => o.Contact == contact && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<OtpCode>> GetUnusedByContactAsync(string contact, OtpPurpose purpose)
    {
        return await _context.OtpCodes
            .Where(o => o.Contact == contact && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
    }
}
