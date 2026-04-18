using System.Text.RegularExpressions;
using Mapster;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Infrastructure.Services.Platform;

public partial class PackageService : IPackageService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricingService;
    private readonly IClock _clock;

    public PackageService(
        IPackageRepository packageRepository,
        IFeatureRepository featureRepository,
        IUnitOfWork unitOfWork,
        IPricingService pricingService,
        IClock clock)
    {
        _packageRepository = packageRepository;
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _clock = clock;
    }

    public async Task<Result<PackageDto>> GetPackageByIdAsync(Guid id)
    {
        var package = await _packageRepository.GetWithFeaturesAsync(id);

        if (package == null || package.IsDeleted)
            return Result<PackageDto>.Failure("Package not found");

        return Result<PackageDto>.Success(package.Adapt<PackageDto>());
    }

    public async Task<Result<List<PackageDto>>> GetPackagesAsync()
    {
        var packages = await _packageRepository.GetAllForAdminAsync();
        return Result<List<PackageDto>>.Success(packages.Select(p => p.Adapt<PackageDto>()).ToList());
    }

    public async Task<Result<PackageDetailDto>> GetPackageWithDetailsAsync(Guid id)
    {
        var package = await _packageRepository.GetWithFeaturesAsync(id);

        if (package == null || package.IsDeleted)
            return Result<PackageDetailDto>.Failure("Package not found");

        var packageDetail = new PackageDetailDto
        {
            Id = package.Id,
            Name = package.Name,
            Code = package.Code,
            Description = package.Description,
            MonthlyPrice = package.MonthlyPrice,
            YearlyPrice = package.YearlyPrice,
            Status = package.Status,
            IsActive = package.Status == PackageStatus.Active,
            IsPublic = package.IsPublic,
            IsCustom = package.IsCustom,
            ForClinicId = package.ForClinicId,
            DisplayOrder = package.DisplayOrder,
            TrialDays = package.TrialDays,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            Features = package.Features
                .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted)
                .Select(pf => new PackageFeatureDto
                {
                    Id = pf.Id,
                    PackageId = pf.PackageId,
                    FeatureId = pf.FeatureId,
                    FeatureName = pf.Feature!.Name,
                    FeatureCode = pf.Feature.Code,
                    FeaturePrice = 0,
                    PricingType = pf.Feature.PricingType,
                    PerUnitPrice = 0,
                    IsIncluded = pf.IsIncluded,
                    Limit = pf.Quantity,
                    CalculatedPrice = pf.CalculatedPrice
                })
                .ToList()
        };

        return Result<PackageDetailDto>.Success(packageDetail);
    }

    public async Task<Result<PackageDto>> CreatePackageAsync(CreatePackageRequestDto request)
    {
        var code = !string.IsNullOrWhiteSpace(request.Code)
            ? request.Code
            : GenerateCodeFromName(request.Name);

        var existingPackage = await _packageRepository.GetByCodeAsync(code);

        if (existingPackage != null && !existingPackage.IsDeleted)
            return Result<PackageDto>.Failure("A package with this code already exists");

        if (request.Features.Count > 0)
        {
            var featureIds = request.Features.Select(f => f.FeatureId).ToList();
            if (featureIds.Count != featureIds.Distinct().Count())
                return Result<PackageDto>.Failure("Duplicate feature IDs are not allowed");
        }

        var package = new Package
        {
            Name = request.Name,
            Code = code,
            Description = request.Description,
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            CalculatedMonthlyPrice = request.CalculatedMonthlyPrice ?? 0,
            CalculatedYearlyPrice = request.CalculatedYearlyPrice ?? 0,
            Status = PackageStatus.Draft,
            IsPublic = request.IsPublic,
            IsCustom = request.IsCustom,
            ForClinicId = request.ForClinicId,
            DisplayOrder = request.DisplayOrder,
            TrialDays = request.TrialDays,
            Tier = request.Tier,
            IsPopular = request.IsPopular,
            CreatedAt = _clock.UtcNow
        };

        foreach (var featureRequest in request.Features)
        {
            var featureExists = await _featureRepository.ExistsAsync(featureRequest.FeatureId);

            if (!featureExists)
                return Result<PackageDto>.Failure($"Feature with ID {featureRequest.FeatureId} not found");

            package.Features.Add(new PackageFeature
            {
                FeatureId = featureRequest.FeatureId,
                IsIncluded = featureRequest.IsIncluded,
                Quantity = featureRequest.Limit,
                CalculatedPrice = featureRequest.CalculatedPrice ?? 0,
                CreatedAt = _clock.UtcNow
            });
        }

        await _packageRepository.AddAsync(package);
        await _unitOfWork.SaveChangesAsync();

        return Result<PackageDto>.Success(package.Adapt<PackageDto>());
    }

    public async Task<Result<PackageDto>> UpdatePackageAsync(Guid id, UpdatePackageRequestDto request)
    {
        var package = await _packageRepository.GetForEditAsync(id);

        if (package == null)
            return Result<PackageDto>.Failure("Package not found");

        package.Name = request.Name;
        package.Description = request.Description;
        package.MonthlyPrice = request.MonthlyPrice;
        package.YearlyPrice = request.YearlyPrice;
        package.CalculatedMonthlyPrice = request.CalculatedMonthlyPrice ?? package.CalculatedMonthlyPrice;
        package.CalculatedYearlyPrice = request.CalculatedYearlyPrice ?? package.CalculatedYearlyPrice;
        package.TrialDays = request.TrialDays ?? package.TrialDays;
        package.DisplayOrder = request.DisplayOrder;
        package.Tier = request.Tier;
        package.IsPopular = request.IsPopular;
        package.UpdatedAt = _clock.UtcNow;

        if (request.Features != null)
        {
            await _packageRepository.ClearFeaturesAsync(id);

            foreach (var featureRequest in request.Features)
            {
                var featureExists = await _featureRepository.ExistsAsync(featureRequest.FeatureId);

                if (!featureExists)
                    return Result<PackageDto>.Failure($"Feature with ID {featureRequest.FeatureId} not found");

                package.Features.Add(new PackageFeature
                {
                    FeatureId = featureRequest.FeatureId,
                    IsIncluded = featureRequest.IsIncluded,
                    Quantity = featureRequest.Limit,
                    CalculatedPrice = featureRequest.CalculatedPrice ?? 0,
                    CreatedAt = _clock.UtcNow
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return Result<PackageDto>.Success(package.Adapt<PackageDto>());
    }

    public async Task<Result> ActivatePackageAsync(Guid id)
    {
        var package = await _packageRepository.GetByIdAsync(id);

        if (package == null || package.IsDeleted)
            return Result.Failure("Package not found");

        if (package.Status != PackageStatus.Draft)
            return Result.Failure("Only draft packages can be activated");

        package.Status = PackageStatus.Active;
        package.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ArchivePackageAsync(Guid id)
    {
        var package = await _packageRepository.GetByIdAsync(id);

        if (package == null || package.IsDeleted)
            return Result.Failure("Package not found");

        if (package.Status != PackageStatus.Active)
            return Result.Failure("Only active packages can be archived");

        package.Status = PackageStatus.Archived;
        package.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<PublicPackageDto>>> GetPublicPackagesAsync()
    {
        var packages = await _packageRepository.GetPublicPackagesAsync();
        var dtos = packages
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => new PublicPackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                MonthlyPrice = p.MonthlyPrice,
                YearlyPrice = p.YearlyPrice,
                TrialDays = p.TrialDays,
                Tier = p.Tier.ToString(),
                IsPopular = p.IsPopular,
                HasMonthly = p.MonthlyPrice > 0,
                HasYearly = p.YearlyPrice > 0,
                Features = p.Features
                    .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted && pf.IsIncluded)
                    .Select(pf => new PublicPackageFeatureDto
                    {
                        FeatureId = pf.FeatureId,
                        Name = pf.Feature!.Name,
                        Code = pf.Feature.Code,
                        Category = pf.Feature.Category?.Name,
                        Limit = pf.Quantity,
                        IconKey = pf.Feature.IconKey
                    }).ToList()
            })
            .ToList();

        return Result<List<PublicPackageDto>>.Success(dtos);
    }

    public async Task<Result> DeletePackageAsync(Guid id)
    {
        var package = await _packageRepository.GetByIdAsync(id);

        if (package == null || package.IsDeleted)
            return Result.Failure("Package not found");

        if (package.Status != PackageStatus.Draft)
            return Result.Failure("Only Draft packages can be deleted");

        var hasSubscriptions = await _packageRepository.HasAnySubscriptionsAsync(id);

        if (hasSubscriptions)
            return Result.Failure("Cannot delete package: it has existing subscription history. Archive it instead.");

        package.IsDeleted = true;
        package.DeletedAt = _clock.UtcNow;
        package.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<PackageDetailDto>> RecalculatePackagePriceAsync(Guid id)
    {
        var package = await _packageRepository.GetWithFeaturesAndPricingAsync(id);

        if (package == null || package.IsDeleted)
            return Result<PackageDetailDto>.Failure("Package not found");

        decimal calculatedMonthlyPrice = 0;
        decimal calculatedYearlyPrice = 0;

        foreach (var packageFeature in package.Features)
        {
            if (packageFeature.Feature == null || !packageFeature.IsIncluded)
                continue;

            var feature = packageFeature.Feature;
            var pricing = feature.PricingHistory
                .Where(p => p.EffectiveDate <= _clock.UtcNow)
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefault(p => !p.ExpiryDate.HasValue || p.ExpiryDate >= _clock.UtcNow);

            if (pricing == null)
                continue;

            var quantity = packageFeature.Quantity ?? 1;
            var unitPrice = feature.PricingType == PricingType.Fixed ? pricing.BasePrice : pricing.PerUnitPrice;
            var calculatedPrice = unitPrice * quantity;

            packageFeature.CalculatedPrice = calculatedPrice;
            packageFeature.UpdatedAt = _clock.UtcNow;

            calculatedMonthlyPrice += calculatedPrice;
            calculatedYearlyPrice += calculatedPrice * 12;
        }

        package.CalculatedMonthlyPrice = calculatedMonthlyPrice;
        package.CalculatedYearlyPrice = calculatedYearlyPrice;
        package.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetPackageWithDetailsAsync(id);
    }

    private static string GenerateCodeFromName(string name)
    {
        var code = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and");
        code = Regex.Replace(code, @"[^a-z0-9-]", "");
        code = Regex.Replace(code, @"-+", "-");
        code = code.Trim('-');
        return code.Length > 50 ? code[..50] : code;
    }

}
