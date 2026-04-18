using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of ISubscriptionRepository
/// </summary>
public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetActiveSubscriptionByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .ThenInclude(p => p.Features)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial));
    }

    public async Task<Subscription?> GetActiveSubscriptionWithDetailsByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(s => s.ClinicId == clinicId)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
            .Where(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
            .Include(s => s.Package)
                .ThenInclude(p => p.Features)
                    .ThenInclude(pf => pf.Feature)
            .Include(s => s.FeatureUsage)
                .ThenInclude(fu => fu.Feature)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription?> GetWithPackageAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<Subscription?> GetFullSubscriptionDetailsAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .ThenInclude(p => p.Features)
            .ThenInclude(pf => pf.Feature)
            .Include(s => s.AddOns)
            .Include(s => s.FeatureUsage)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetExpiringAsync(int daysThreshold)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

        return await _dbSet
            .Include(s => s.Clinic)
            .Include(s => s.Package)
            .Where(s => s.EndDate <= thresholdDate &&
                s.EndDate > DateTime.UtcNow &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetOverdueAsync()
    {
        return await _dbSet
            .Include(s => s.Clinic)
            .Where(s => s.Status == SubscriptionStatus.Suspended && s.EndDate < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<Subscription?> GetWithCurrentUsageAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.FeatureUsage)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<IEnumerable<Subscription>> GetByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .Where(s => s.ClinicId == clinicId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetWithAddOnAsync(Guid featureId)
    {
        return await _dbSet
            .Include(s => s.AddOns)
            .Where(s => s.AddOns.Any(ao => ao.FeatureId == featureId && ao.Status == AddOnStatus.Active))
            .ToListAsync();
    }

    public async Task<int> CountActiveByPackageAsync(Guid packageId)
    {
        return await _dbSet
            .CountAsync(s => s.PackageId == packageId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial));
    }

    public async Task<IEnumerable<Subscription>> GetDueForInvoiceGenerationAsync(int daysThreshold = 7)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

        return await _dbSet
            .Include(s => s.Package)
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.AutoRenew
                && s.EndDate <= thresholdDate)
            .ToListAsync();
    }

    public async Task<Subscription?> GetWithPackageAndClinicAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .Include(s => s.Clinic)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<Subscription?> GetWithFeatureUsageAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.Package)
            .Include(s => s.FeatureUsage)
                .ThenInclude(sf => sf.Feature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<Subscription?> GetActiveByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.Status == SubscriptionStatus.Active);
    }

    public async Task<Subscription?> GetForUpgradeAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Include(s => s.FeatureUsage)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<Subscription?> GetForModificationAsync(Guid subscriptionId)
    {
        return await _dbSet
            .Where(s => s.Id == subscriptionId && s.Status == SubscriptionStatus.Active)
            .Include(s => s.Package)
                .ThenInclude(p => p.Features)
            .Include(s => s.FeatureUsage)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Subscription>> GetAllWithPackageAndClinicAsync(Guid? clinicId = null)
    {
        var query = _dbSet
            .Include(s => s.Package)
            .Include(s => s.Clinic)
            .AsQueryable();

        if (clinicId.HasValue)
            query = query.Where(x => x.ClinicId == clinicId.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Subscription> Subscriptions, int TotalCount)> GetPagedWithPackageAndClinicAsync(
        int page, int pageSize, SubscriptionStatus? status = null, Guid? clinicId = null)
    {
        var query = _dbSet
            .Include(s => s.Package)
            .Include(s => s.Clinic)
            .Where(s => !s.Clinic.IsDeleted)
            .AsQueryable();

        if (clinicId.HasValue)
            query = query.Where(x => x.ClinicId == clinicId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var totalCount = await query.CountAsync();

        var subscriptions = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (subscriptions, totalCount);
    }

    public async Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync()
    {
        return await _dbSet
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync();
    }
}
