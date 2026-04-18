using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Clinic;

public class TenantResolutionService : ITenantResolutionService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public TenantResolutionService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<(ClinicStatus Status, bool Exists)?> ResolveClinicAsync(Guid tenantId)
    {
        var cacheKey = $"ClinicStatus:{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == tenantId);
            return clinic != null ? (clinic.Status, true) : (default, false);
        });
    }
}
