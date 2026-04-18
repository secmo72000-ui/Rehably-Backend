using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IUsageRecordRepository
/// </summary>
public class UsageRecordRepository : Repository<UsageRecord>, IUsageRecordRepository
{
    public UsageRecordRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UsageRecord?> GetByClinicMetricPeriodAsync(Guid clinicId, UsageMetric metric, DateTime period)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.ClinicId == clinicId && u.Metric == metric && u.Period == period);
    }

    public async Task<IEnumerable<UsageRecord>> GetByClinicAndPeriodAsync(Guid clinicId, DateTime period)
    {
        return await _dbSet
            .Where(u => u.ClinicId == clinicId && u.Period == period)
            .ToListAsync();
    }

    public async Task<IEnumerable<UsageRecord>> GetPreviousPeriodAsync(Guid clinicId)
    {
        var now = DateTime.UtcNow;
        var previousPeriod = now.Month == 1
            ? new DateTime(now.Year - 1, 12, 1)
            : new DateTime(now.Year, now.Month - 1, 1);

        return await _dbSet
            .Where(u => u.ClinicId == clinicId && u.Period == previousPeriod)
            .ToListAsync();
    }
}
