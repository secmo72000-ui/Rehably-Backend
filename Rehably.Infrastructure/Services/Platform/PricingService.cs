using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Pricing;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Platform;

public class PricingService : IPricingService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IClock _clock;

    public PricingService(IPackageRepository packageRepository, IClock clock)
    {
        _packageRepository = packageRepository;
        _clock = clock;
    }

    public async Task<Result<PackageSnapshotDto>> CreatePackageSnapshotAsync(Guid packageId)
    {
        var package = await _packageRepository.GetWithFeaturesAsync(packageId);

        if (package == null || package.IsDeleted)
            return Result<PackageSnapshotDto>.Failure("Package not found");

        var snapshot = new PackageSnapshotDto
        {
            PackageId = package.Id,
            PackageName = package.Name,
            PackageCode = package.Code,
            MonthlyPrice = package.MonthlyPrice,
            YearlyPrice = package.YearlyPrice,
            CalculatedMonthlyPrice = package.CalculatedMonthlyPrice,
            CalculatedYearlyPrice = package.CalculatedYearlyPrice,
            TrialDays = package.TrialDays,
            IsPublic = package.IsPublic,
            IsCustom = package.IsCustom,
            ForClinicId = package.ForClinicId,
            SnapshotDate = _clock.UtcNow,
            Features = package.Features
                .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted)
                .Select(pf => new PackageFeatureSnapshotDto
                {
                    FeatureId = pf.FeatureId,
                    FeatureName = pf.Feature!.Name,
                    FeatureCode = pf.Feature.Code,
                    Limit = pf.Quantity,
                    IsIncluded = pf.IsIncluded,
                    CalculatedPrice = pf.CalculatedPrice,
                    PricingType = pf.Feature.PricingType
                })
                .ToList()
        };

        return Result<PackageSnapshotDto>.Success(snapshot);
    }

    public async Task<Result<decimal>> CalculatePackagePriceAsync(Guid packageId, PricingCalculationRequestDto request)
    {
        var breakdownResult = await GetPricingBreakdownAsync(packageId, request);

        if (!breakdownResult.IsSuccess)
            return Result<decimal>.Failure(breakdownResult.Error);

        return Result<decimal>.Success(breakdownResult.Value.TotalPrice);
    }

    public async Task<Result<PackagePricingBreakdownDto>> GetPricingBreakdownAsync(Guid packageId, PricingCalculationRequestDto request)
    {
        var package = await _packageRepository.GetWithFeaturesAndPricingAsync(packageId, activeOnly: true);

        if (package == null)
            return Result<PackagePricingBreakdownDto>.Failure("Package not found or inactive");

        var featurePricingList = new List<FeaturePricingItemDto>();
        var featuresTotal = 0m;
        var userTierPrice = 0m;
        var storagePrice = 0m;

        foreach (var packageFeature in package.Features)
        {
            if (packageFeature.Feature == null || packageFeature.Feature.IsDeleted || !packageFeature.IsIncluded)
                continue;

            var feature = packageFeature.Feature;
            var pricing = GetCurrentFeaturePricing(feature);

            if (pricing == null)
                continue;

            var quantity = packageFeature.Quantity ?? 1;
            if (request.FeatureQuantities != null && request.FeatureQuantities.TryGetValue(feature.Id, out var customQuantity))
            {
                quantity = customQuantity;
            }

            var unitPrice = feature.PricingType == PricingType.Fixed ? pricing.BasePrice : pricing.PerUnitPrice;
            var subtotal = unitPrice * quantity;

            featurePricingList.Add(new FeaturePricingItemDto
            {
                FeatureId = feature.Id,
                FeatureName = feature.Name,
                FeatureCode = feature.Code,
                PricingType = feature.PricingType,
                UnitPrice = unitPrice,
                Quantity = quantity,
                Subtotal = subtotal
            });

            featuresTotal += subtotal;

            if (feature.PricingType == PricingType.PerUser)
                userTierPrice += subtotal;

            if (feature.PricingType == PricingType.PerStorageGB)
                storagePrice += subtotal;
        }

        var breakdown = new PackagePricingBreakdownDto
        {
            BasePrice = 0,
            FeaturesTotal = featuresTotal,
            UserTierPrice = userTierPrice,
            StoragePrice = storagePrice,
            TotalPrice = featuresTotal,
            FeaturePricing = featurePricingList
        };

        return Result<PackagePricingBreakdownDto>.Success(breakdown);
    }

    public async Task<Result<PackageSnapshotDto>> CalculateModifiedPackagePriceAsync(Guid packageId, List<PackageFeatureDto> modifiedFeatures)
    {
        var package = await _packageRepository.GetByIdAsync(packageId);

        if (package == null || package.IsDeleted)
            return Result<PackageSnapshotDto>.Failure("Package not found");

        var snapshot = new PackageSnapshotDto
        {
            PackageId = package.Id,
            PackageName = package.Name,
            PackageCode = package.Code,
            MonthlyPrice = package.MonthlyPrice,
            YearlyPrice = package.YearlyPrice,
            CalculatedMonthlyPrice = package.CalculatedMonthlyPrice,
            CalculatedYearlyPrice = package.CalculatedYearlyPrice,
            TrialDays = package.TrialDays,
            IsPublic = package.IsPublic,
            IsCustom = package.IsCustom,
            ForClinicId = package.ForClinicId,
            SnapshotDate = _clock.UtcNow,
            Features = modifiedFeatures.Select(f => new PackageFeatureSnapshotDto
            {
                FeatureId = f.FeatureId,
                FeatureName = f.FeatureName,
                FeatureCode = f.FeatureCode,
                Limit = f.Limit,
                IsIncluded = f.IsIncluded,
                CalculatedPrice = f.CalculatedPrice,
                PricingType = f.PricingType
            }).ToList()
        };

        return Result<PackageSnapshotDto>.Success(snapshot);
    }

    private FeaturePricing? GetCurrentFeaturePricing(Feature feature)
    {
        return feature.PricingHistory
            .Where(p => p.EffectiveDate <= _clock.UtcNow)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault(p => !p.ExpiryDate.HasValue || p.ExpiryDate >= _clock.UtcNow);
    }
}
