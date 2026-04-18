using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

/// <summary>
/// Implementation of IClinicRepository
/// </summary>
public class ClinicRepository : Repository<Clinic>, IClinicRepository
{
    public ClinicRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Clinic?> GetBySubdomainAsync(string subdomain)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Slug == subdomain.ToLowerInvariant());
    }

    public async Task<Clinic?> GetWithSubscriptionAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(c => c.CurrentSubscription)
                .ThenInclude(s => s!.Package)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(pf => pf.Feature)
            .Include(c => c.Documents)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == clinicId);
    }

    public async Task<IEnumerable<Clinic>> GetActiveClinicsAsync()
    {
        return await _dbSet
            .Where(c => c.Status == ClinicStatus.Active)
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetByStatusAsync(ClinicStatus status)
    {
        return await _dbSet
            .Where(c => c.Status == status)
            .ToListAsync();
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeClinicId = null)
    {
        var query = _dbSet.Where(c => c.Slug == subdomain.ToLowerInvariant());

        if (excludeClinicId.HasValue)
        {
            query = query.Where(c => c.Id != excludeClinicId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<int> CountBySubscriptionAsync(Guid subscriptionId)
    {
        return await _dbSet
            .CountAsync(c => c.CurrentSubscriptionId == subscriptionId);
    }

    public async Task<IEnumerable<Clinic>> GetExpiringSubscriptionsAsync(int daysThreshold)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

        return await _dbSet
            .Include(c => c.CurrentSubscription)
            .AsSplitQuery()
            .Where(c => c.CurrentSubscription != null &&
                c.CurrentSubscription.EndDate <= thresholdDate &&
                c.CurrentSubscription.EndDate > DateTime.UtcNow &&
                (c.CurrentSubscription.Status == SubscriptionStatus.Active ||
                 c.CurrentSubscription.Status == SubscriptionStatus.Trial))
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, string>> GetClinicNamesByIdsAsync(IEnumerable<Guid> clinicIds)
    {
        var idsList = clinicIds.ToList();
        return await _dbSet
            .Where(c => idsList.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
    }

    public async Task<Clinic?> GetWithSubscriptionAndPackageAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(c => c.CurrentSubscription)
                .ThenInclude(s => s!.Package)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(pf => pf.Feature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == clinicId);
    }

    public async Task<(List<Clinic> Clinics, int TotalCount)> SearchAsync(
        string? search = null,
        ClinicStatus? status = null,
        SubscriptionStatus? subscriptionStatus = null,
        Guid? packageId = null,
        bool includeDeleted = false,
        string? sortBy = null,
        bool sortDesc = true,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbSet
            .Include(c => c.CurrentSubscription)
                .ThenInclude(s => s!.Package)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(pf => pf.Feature)
            .Include(c => c.Documents)
            .AsSplitQuery()
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(normalizedSearch) ||
                (c.NameArabic != null && c.NameArabic.ToLower().Contains(normalizedSearch)) ||
                (c.City != null && c.City.ToLower().Contains(normalizedSearch)) ||
                (c.Email != null && c.Email.ToLower().Contains(normalizedSearch)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (subscriptionStatus.HasValue)
        {
            query = query.Where(c => c.CurrentSubscription != null && c.CurrentSubscription.Status == subscriptionStatus.Value);
        }

        if (packageId.HasValue)
        {
            query = query.Where(c =>
                c.CurrentSubscription != null &&
                c.CurrentSubscription.PackageId == packageId.Value &&
                (c.CurrentSubscription.Status == SubscriptionStatus.Active || c.CurrentSubscription.Status == SubscriptionStatus.Trial));
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "status" => sortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            _ => sortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var clinics = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (clinics, totalCount);
    }

    public async Task<List<Clinic>> GetPendingApprovalAsync()
    {
        return await _dbSet
            .Include(c => c.CurrentSubscription)
                .ThenInclude(s => s!.Package)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(pf => pf.Feature)
            .Include(c => c.Onboarding)
            .AsSplitQuery()
            .Where(c => !c.IsDeleted && c.Status == ClinicStatus.Active &&
                   c.Onboarding != null && c.Onboarding.CurrentStep == OnboardingStep.PendingApproval)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Clinic?> GetWithActiveSubscriptionAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(c => c.CurrentSubscription)
                .ThenInclude(s => s!.Package)
            .FirstOrDefaultAsync(c => c.Id == clinicId);
    }

    public async Task<Clinic?> GetWithDocumentsAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == clinicId, cancellationToken);
    }
}
