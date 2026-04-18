using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using System.Text.Json;

namespace Rehably.Infrastructure.Services.Platform;

public class SubscriptionModificationService : ISubscriptionModificationService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IPlatformSubscriptionService _platformSubscriptionService;
    private readonly IClock _clock;

    public SubscriptionModificationService(
        ISubscriptionRepository subscriptionRepository,
        IPackageRepository packageRepository,
        IFeatureRepository featureRepository,
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IPlatformSubscriptionService platformSubscriptionService,
        IClock clock)
    {
        _subscriptionRepository = subscriptionRepository;
        _packageRepository = packageRepository;
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _platformSubscriptionService = platformSubscriptionService;
        _clock = clock;
    }

    public async Task<Result<SubscriptionDetailDto>> UpgradeSubscriptionAsync(Guid id, UpgradeSubscriptionRequestDto request)
    {
        var subscription = await _subscriptionRepository.GetForUpgradeAsync(id);

        if (subscription == null)
            return Result<SubscriptionDetailDto>.Failure("Subscription not found");

        var newPackage = await _packageRepository.GetWithFeaturesAsync(request.NewPackageId);

        if (newPackage == null || newPackage.IsDeleted || newPackage.Status != PackageStatus.Active)
            return Result<SubscriptionDetailDto>.Failure("New package not found or inactive");

        var priceSnapshotResult = await _pricingService.CreatePackageSnapshotAsync(request.NewPackageId);
        if (!priceSnapshotResult.IsSuccess)
            return Result<SubscriptionDetailDto>.Failure("Failed to create price snapshot");

        subscription.PackageId = request.NewPackageId;
        subscription.PriceSnapshot = JsonSerializer.Serialize(priceSnapshotResult.Value);
        subscription.UpdatedAt = _clock.UtcNow;

        var usageRepo = _unitOfWork.Repository<SubscriptionFeatureUsage>();
        foreach (var usage in subscription.FeatureUsage.ToList())
        {
            await usageRepo.DeleteAsync(usage);
        }

        foreach (var packageFeature in newPackage.Features)
        {
            if (!packageFeature.IsIncluded || packageFeature.Feature == null)
                continue;

            var limit = packageFeature.Quantity ?? int.MaxValue;

            subscription.FeatureUsage.Add(new SubscriptionFeatureUsage
            {
                FeatureId = packageFeature.FeatureId,
                Limit = limit,
                Used = 0,
                LastResetAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                Feature = packageFeature.Feature
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return await _platformSubscriptionService.GetSubscriptionWithDetailsAsync(id);
    }

    public async Task<Result<SubscriptionModificationResultDto>> ModifySubscriptionAsync(Guid id, ModifySubscriptionRequestDto request)
    {
        var subscription = await _subscriptionRepository.GetForModificationAsync(id);

        if (subscription == null)
            return Result<SubscriptionModificationResultDto>.Failure("Subscription not found or not active");

        var currentSnapshot = await _pricingService.CreatePackageSnapshotAsync(subscription.PackageId);
        if (!currentSnapshot.IsSuccess)
            return Result<SubscriptionModificationResultDto>.Failure("Failed to load current package snapshot");

        var previousPrice = currentSnapshot.Value.TotalMonthlyPrice;
        var changesApplied = new List<string>();
        var featureIdsInSubscription = subscription.FeatureUsage.Select(sf => sf.FeatureId).ToHashSet();

        var usageRepo = _unitOfWork.Repository<SubscriptionFeatureUsage>();
        foreach (var featureId in request.RemoveFeatureIds)
        {
            if (!featureIdsInSubscription.Contains(featureId))
                continue;

            var usageToRemove = subscription.FeatureUsage.FirstOrDefault(sf => sf.FeatureId == featureId);
            if (usageToRemove != null)
            {
                subscription.FeatureUsage.Remove(usageToRemove);
                await usageRepo.DeleteAsync(usageToRemove);
                changesApplied.Add($"Removed feature ID: {featureId}");
            }
        }

        var availableFeatures = await _featureRepository.GetActiveFeaturesAsync();
        var featureIdToFeature = availableFeatures.ToDictionary(f => f.Id);

        foreach (var featureId in request.AddFeatureIds)
        {
            if (featureIdsInSubscription.Contains(featureId))
                continue;

            if (!featureIdToFeature.TryGetValue(featureId, out var feature))
                return Result<SubscriptionModificationResultDto>.Failure($"Feature ID {featureId} not found or inactive");

            var packageFeature = subscription.Package?.Features
                .FirstOrDefault(pf => pf.FeatureId == featureId && pf.IsIncluded);
            var limit = packageFeature?.Quantity ?? 1;

            subscription.FeatureUsage.Add(new SubscriptionFeatureUsage
            {
                FeatureId = featureId,
                Limit = limit,
                Used = 0,
                LastResetAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                Feature = feature
            });

            changesApplied.Add($"Added feature: {feature.Name}");
            featureIdsInSubscription.Add(featureId);
        }

        foreach (var quantityUpdate in request.UpdateQuantities)
        {
            if (!featureIdsInSubscription.Contains(quantityUpdate.FeatureId))
                continue;

            var usage = subscription.FeatureUsage.FirstOrDefault(sf => sf.FeatureId == quantityUpdate.FeatureId);
            if (usage != null && featureIdToFeature.TryGetValue(quantityUpdate.FeatureId, out var feature))
            {
                var oldLimit = usage.Limit;
                usage.Limit = quantityUpdate.NewQuantity;
                usage.UpdatedAt = _clock.UtcNow;
                changesApplied.Add($"Updated {feature.Name} quantity: {oldLimit} → {quantityUpdate.NewQuantity}");
            }
        }

        subscription.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var updatedSnapshotResult = await _pricingService.CalculateModifiedPackagePriceAsync(
            subscription.PackageId,
            subscription.FeatureUsage.Select(sf => new PackageFeatureDto
            {
                Id = Guid.NewGuid(),
                PackageId = subscription.PackageId,
                FeatureId = sf.FeatureId,
                FeatureName = sf.Feature?.Name ?? string.Empty,
                FeatureCode = sf.Feature?.Code ?? string.Empty,
                FeaturePrice = 0,
                PricingType = sf.Feature?.PricingType ?? PricingType.Fixed,
                PerUnitPrice = null,
                CalculatedPrice = 0,
                IsIncluded = true
            }).ToList()
        );

        decimal newPrice = updatedSnapshotResult.IsSuccess ? updatedSnapshotResult.Value.TotalMonthlyPrice : previousPrice;

        var detailResult = await _platformSubscriptionService.GetSubscriptionWithDetailsAsync(id);
        if (!detailResult.IsSuccess)
            return Result<SubscriptionModificationResultDto>.Failure(detailResult.Error);

        var result = new SubscriptionModificationResultDto
        {
            Subscription = detailResult.Value,
            PreviousPrice = previousPrice,
            NewPrice = newPrice,
            ChangesApplied = changesApplied
        };

        return Result<SubscriptionModificationResultDto>.Success(result);
    }

    public async Task<Result<SubscriptionModificationResultDto>> PreviewModificationAsync(
        Guid subscriptionId, ModifySubscriptionRequestDto request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await ModifySubscriptionAsync(subscriptionId, request);

            await _unitOfWork.RollbackTransactionAsync();

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
