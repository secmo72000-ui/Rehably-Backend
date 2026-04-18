using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Platform;

public class FeaturePricingService : IFeaturePricingService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeaturePricingRepository _featurePricingRepository;
    private readonly IPackageFeatureRepository _packageFeatureRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IFeatureCategoryRepository _featureCategoryRepository;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string FeaturesCacheKey = "Features:All";
    private static readonly TimeSpan FeaturesCacheDuration = TimeSpan.FromHours(1);

    public FeaturePricingService(
        IFeatureRepository featureRepository,
        IFeaturePricingRepository featurePricingRepository,
        IPackageFeatureRepository packageFeatureRepository,
        ISubscriptionRepository subscriptionRepository,
        IFeatureCategoryRepository featureCategoryRepository,
        IMemoryCache cache,
        IUnitOfWork unitOfWork)
    {
        _featureRepository = featureRepository;
        _featurePricingRepository = featurePricingRepository;
        _packageFeatureRepository = packageFeatureRepository;
        _subscriptionRepository = subscriptionRepository;
        _featureCategoryRepository = featureCategoryRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FeaturePricingDto>> GetCurrentPricingAsync(Guid featureId)
    {
        var feature = await _featureRepository.GetByIdAsync(featureId);

        if (feature == null || feature.IsDeleted)
            return Result<FeaturePricingDto>.Failure("Feature not found");

        var pricing = await _featurePricingRepository.GetCurrentPricingAsync(featureId);

        if (pricing == null)
            return Result<FeaturePricingDto>.Failure("No pricing found for feature");

        return Result<FeaturePricingDto>.Success(pricing.Adapt<FeaturePricingDto>());
    }

    public async Task<Result<FeaturePricingDto>> SetFeaturePricingAsync(Guid featureId, SetFeaturePricingRequestDto request)
    {
        var feature = await _featureRepository.GetByIdAsync(featureId);

        if (feature == null || feature.IsDeleted)
            return Result<FeaturePricingDto>.Failure("Feature not found");

        var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow;

        var existingPricing = await _featurePricingRepository.GetPricingAtDateAsync(featureId, effectiveDate);

        if (existingPricing != null)
        {
            existingPricing.ExpiryDate = effectiveDate;
            await _featurePricingRepository.UpdateAsync(existingPricing);
        }

        var newPricing = new FeaturePricing
        {
            FeatureId = featureId,
            BasePrice = request.BasePrice,
            PerUnitPrice = request.PerUnitPrice,
            EffectiveDate = effectiveDate,
            ExpiryDate = request.ExpiryDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _featurePricingRepository.AddAsync(newPricing);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateFeaturesCacheAsync();

        return Result<FeaturePricingDto>.Success(newPricing.Adapt<FeaturePricingDto>());
    }

    public async Task<Result<List<FeaturePricingDto>>> GetPricingHistoryAsync(Guid featureId)
    {
        var feature = await _featureRepository.GetByIdAsync(featureId);

        if (feature == null || feature.IsDeleted)
            return Result<List<FeaturePricingDto>>.Failure("Feature not found");

        var pricingHistory = await _featurePricingRepository.GetPricingHistoryAsync(featureId);

        var dtos = pricingHistory.Select(p => p.Adapt<FeaturePricingDto>()).ToList();

        return Result<List<FeaturePricingDto>>.Success(dtos);
    }

    public async Task<Result> CanDeactivateFeatureAsync(Guid id)
    {
        var feature = await _featureRepository.GetByIdAsync(id);

        if (feature == null || feature.IsDeleted)
            return Result.Failure("Feature not found");

        var inActivePackages = await _packageFeatureRepository.IsFeatureInActivePackagesAsync(id);

        if (inActivePackages)
            return Result.Failure("Feature is in use by active packages");

        var activeSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsAsync();

        foreach (var subscription in activeSubscriptions)
        {
            if (IsFeatureInSubscriptionSnapshot(subscription, id))
                return Result.Failure("Feature is in use by active subscriptions");
        }

        return Result.Success();
    }

    private static bool IsFeatureInSubscriptionSnapshot(Subscription subscription, Guid featureId)
    {
        if (string.IsNullOrWhiteSpace(subscription.PriceSnapshot))
            return false;

        return subscription.PriceSnapshot.Contains(featureId.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task InvalidateFeaturesCacheAsync()
    {
        _cache.Remove(FeaturesCacheKey);
        var categoryIds = await _featureCategoryRepository.GetActiveCategoryIdsAsync();

        foreach (var categoryId in categoryIds)
        {
            _cache.Remove($"Features:Category:{categoryId}");
        }
    }

}
