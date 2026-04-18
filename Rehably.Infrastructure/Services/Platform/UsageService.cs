using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.DTOs.Usage;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Platform;

public class UsageService : IUsageService
{
    private const string FeatureCodeUsers = "users";
    private const string FeatureCodeStorage = "storage";

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionFeatureUsageRepository _usageRepository;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<UsageService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public UsageService(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionFeatureUsageRepository usageRepository,
        IMemoryCache cache,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<UsageService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _usageRepository = usageRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<bool>> CanUseFeatureAsync(Guid tenantId, string featureCode)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionWithDetailsByClinicIdAsync(tenantId);

            if (subscription == null)
                return Result<bool>.Failure("No active subscription found");

            var packageFeature = subscription.Package.Features
                .FirstOrDefault(pf => pf.Feature.Code == featureCode);

            if (packageFeature == null || !packageFeature.IsIncluded)
                return Result<bool>.Failure("Feature not included in subscription package");

            var limit = GetFeatureLimit(subscription.Package, featureCode);
            var usage = subscription.FeatureUsage
                .FirstOrDefault(fu => fu.Feature.Code == featureCode);

            var used = usage?.Used ?? 0;

            if (limit <= 0)
                return Result<bool>.Success(true);

            return Result<bool>.Success(used < limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check feature usage for tenant {TenantId}, feature {FeatureCode}", tenantId, featureCode);
            return Result<bool>.Failure("Error checking feature usage");
        }
    }

    public async Task<Result<bool>> IncrementUsageAsync(Guid tenantId, string featureCode, int amount = 1)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionWithDetailsByClinicIdAsync(tenantId);

            if (subscription == null)
                return Result<bool>.Failure("No active subscription found");

            var packageFeature = subscription.Package.Features
                .FirstOrDefault(pf => pf.Feature.Code == featureCode);

            if (packageFeature == null || !packageFeature.IsIncluded)
                return Result<bool>.Failure("Feature not included in subscription package");

            var limit = GetFeatureLimit(subscription.Package, featureCode);
            var featureId = packageFeature.FeatureId;
            var usage = subscription.FeatureUsage
                .FirstOrDefault(fu => fu.FeatureId == featureId);

            if (usage == null)
            {
                if (limit > 0 && amount > limit)
                    return Result<bool>.Failure($"Feature usage limit exceeded. Limit: {limit}, Requested: {amount}");

                usage = new SubscriptionFeatureUsage
                {
                    SubscriptionId = subscription.Id,
                    FeatureId = featureId,
                    Limit = limit,
                    Used = amount,
                    LastResetAt = _clock.UtcNow
                };
                await _usageRepository.AddAsync(usage);
            }
            else
            {
                if (limit > 0 && usage.Used + amount > limit)
                    return Result<bool>.Failure($"Feature usage limit exceeded. Limit: {limit}, Current: {usage.Used}, Requested: {amount}");

                usage.Used += amount;
                usage.UpdatedAt = _clock.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
            InvalidateUsageCache(subscription.Id, tenantId, featureCode);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment usage for tenant {TenantId}, feature {FeatureCode}, amount {Amount}", tenantId, featureCode, amount);
            return Result<bool>.Failure("Error incrementing usage");
        }
    }

    public async Task<Result<SubscriptionFeatureUsageDto>> GetUsageAsync(Guid tenantId, string featureCode)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionWithDetailsByClinicIdAsync(tenantId);

            if (subscription == null)
                return Result<SubscriptionFeatureUsageDto>.Failure("No active subscription found");

            var limit = GetFeatureLimit(subscription.Package, featureCode);
            var usage = subscription.FeatureUsage
                .FirstOrDefault(fu => fu.Feature.Code == featureCode);

            if (usage == null)
                return Result<SubscriptionFeatureUsageDto>.Failure("Usage not found for this feature");

            var dto = new SubscriptionFeatureUsageDto
            {
                Id = usage.Id,
                FeatureCode = usage.Feature.Code,
                FeatureName = usage.Feature.Name,
                Limit = limit,
                Used = usage.Used,
                LastResetAt = usage.LastResetAt
            };

            return Result<SubscriptionFeatureUsageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage for tenant {TenantId}, feature {FeatureCode}", tenantId, featureCode);
            return Result<SubscriptionFeatureUsageDto>.Failure("Error getting usage");
        }
    }

    public async Task<Result<List<SubscriptionFeatureUsageDto>>> GetAllUsageAsync(Guid tenantId)
    {
        try
        {
            var cacheKey = $"usage:all:{tenantId}";
            if (_cache.TryGetValue(cacheKey, out List<SubscriptionFeatureUsageDto>? cached))
                return Result<List<SubscriptionFeatureUsageDto>>.Success(cached!);

            var subscription = await _subscriptionRepository.GetActiveSubscriptionWithDetailsByClinicIdAsync(tenantId);

            if (subscription == null)
                return Result<List<SubscriptionFeatureUsageDto>>.Failure("No active subscription found");

            var dtos = subscription.FeatureUsage.Select(usage =>
            {
                var limit = GetFeatureLimit(subscription.Package, usage.Feature.Code);
                return new SubscriptionFeatureUsageDto
                {
                    Id = usage.Id,
                    FeatureCode = usage.Feature.Code,
                    FeatureName = usage.Feature.Name,
                    Limit = limit,
                    Used = usage.Used,
                    LastResetAt = usage.LastResetAt
                };
            }).ToList();

            _cache.Set(cacheKey, dtos, CacheDuration);
            return Result<List<SubscriptionFeatureUsageDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all usage for tenant {TenantId}", tenantId);
            return Result<List<SubscriptionFeatureUsageDto>>.Failure("Error getting all usage");
        }
    }

    public async Task<Result> ResetFeatureUsageAsync(Guid subscriptionId, Guid featureId)
    {
        try
        {
            var usage = await _usageRepository.GetBySubscriptionAndFeatureAsync(subscriptionId, featureId);

            if (usage == null)
                return Result.Failure("Usage record not found");

            usage.Used = 0;
            usage.LastResetAt = _clock.UtcNow;
            usage.UpdatedAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            InvalidateUsageCache(subscriptionId, subscription?.ClinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset feature usage for subscription {SubscriptionId}, feature {FeatureId}", subscriptionId, featureId);
            return Result.Failure("Error resetting usage");
        }
    }

    public async Task<Result> ResetAllUsageAsync(Guid subscriptionId)
    {
        try
        {
            var usages = await _usageRepository.GetBySubscriptionIdAsync(subscriptionId);

            foreach (var usage in usages)
            {
                usage.Used = 0;
                usage.LastResetAt = _clock.UtcNow;
                usage.UpdatedAt = _clock.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            InvalidateUsageCache(subscriptionId, subscription?.ClinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset all usage for subscription {SubscriptionId}", subscriptionId);
            return Result.Failure("Error resetting all usage");
        }
    }

    public async Task<Result<Dictionary<string, UsageStatsDto>>> GetUsageStatsAsync(Guid tenantId)
    {
        try
        {
            var cacheKey = $"usage:stats:{tenantId}";
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, UsageStatsDto>? cached))
                return Result<Dictionary<string, UsageStatsDto>>.Success(cached!);

            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(tenantId);

            if (subscription == null)
                return Result<Dictionary<string, UsageStatsDto>>.Failure("No active subscription found");

            var stats = new Dictionary<string, UsageStatsDto>();

            foreach (var packageFeature in subscription.Package.Features.Where(pf => pf.IsIncluded))
            {
                var featureCode = packageFeature.Feature.Code;
                var limit = GetFeatureLimit(subscription.Package, featureCode);
                var usage = subscription.FeatureUsage
                    .FirstOrDefault(fu => fu.FeatureId == packageFeature.FeatureId);

                var used = usage?.Used ?? 0;

                stats[featureCode] = new UsageStatsDto
                {
                    Limit = limit,
                    Used = used
                };
            }

            _cache.Set(cacheKey, stats, CacheDuration);
            return Result<Dictionary<string, UsageStatsDto>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats for tenant {TenantId}", tenantId);
            return Result<Dictionary<string, UsageStatsDto>>.Failure("Error getting usage stats");
        }
    }

    private static int GetFeatureLimit(Package package, string featureCode)
    {
        var packageFeature = package.Features
            .FirstOrDefault(pf => pf.Feature.Code == featureCode && pf.IsIncluded);

        return packageFeature?.Quantity ?? 0;
    }

    private void InvalidateUsageCache(Guid subscriptionId, Guid? tenantId = null, string? featureCode = null)
    {
        if (!string.IsNullOrEmpty(featureCode))
        {
            _cache.Remove($"usage:{subscriptionId}:{featureCode}");
        }
        else
        {
            _cache.Remove($"usage:{subscriptionId}");
        }

        if (tenantId.HasValue)
        {
            _cache.Remove($"usage:all:{tenantId.Value}");
            _cache.Remove($"usage:stats:{tenantId.Value}");
        }
    }
}
