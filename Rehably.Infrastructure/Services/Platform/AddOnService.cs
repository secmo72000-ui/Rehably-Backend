using Mapster;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Constants;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Platform;

public class AddOnService : IAddOnService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionAddOnRepository _addOnRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<AddOnService> _logger;

    public AddOnService(
        IFeatureRepository featureRepository,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionAddOnRepository addOnRepository,
        IPackageRepository packageRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AddOnService> logger)
    {
        _featureRepository = featureRepository;
        _subscriptionRepository = subscriptionRepository;
        _addOnRepository = addOnRepository;
        _packageRepository = packageRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<List<FeatureDto>>> GetAddOnFeaturesAsync()
    {
        try
        {
            var features = await _featureRepository.GetAddOnFeaturesAsync();

            var dtos = features.Adapt<List<FeatureDto>>();

            return Result<List<FeatureDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get add-on features");
            return Result<List<FeatureDto>>.Failure("Failed to get add-on features");
        }
    }

    public async Task<Result> SetFeatureAddOnStatusAsync(Guid featureId, bool isAddOn)
    {
        try
        {
            var feature = await _featureRepository.GetByIdAsync(featureId);
            if (feature == null)
            {
                return Result.Failure("Feature not found");
            }

            feature.IsAddOn = isAddOn;
            feature.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Feature {FeatureId} add-on status set to {IsAddOn}", featureId, isAddOn);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set feature {FeatureId} add-on status", featureId);
            return Result.Failure("Failed to update feature");
        }
    }

    public async Task<Result<List<AvailableAddOnDto>>> GetAvailableAddOnsAsync(Guid clinicId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (subscription == null)
                return Result<List<AvailableAddOnDto>>.Failure("No active subscription found for clinic");

            var package_ = await _packageRepository.GetWithFeaturesAsync(subscription.PackageId);
            if (package_ == null)
                return Result<List<AvailableAddOnDto>>.Failure("Package not found");

            var activeAddOns = await _addOnRepository.GetActiveBySubscriptionIdAsync(subscription.Id);
            var addonLimitsByFeature = activeAddOns
                .GroupBy(a => a.FeatureId)
                .ToDictionary(g => g.Key, g => g.Sum(a => a.Quantity));

            var availableAddOns = package_.Features
                .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted && pf.IsIncluded)
                .Select(pf => new AvailableAddOnDto
                {
                    FeatureId = pf.FeatureId,
                    FeatureName = pf.Feature!.Name,
                    FeatureCode = pf.Feature.Code,
                    Description = pf.Feature.Description,
                    PricingType = pf.Feature.PricingType,
                    BasePrice = 0,
                    PerUnitPrice = 0,
                    MinQuantity = 1,
                    MaxQuantity = 10000,
                    CurrentAddonLimit = addonLimitsByFeature.GetValueOrDefault(pf.FeatureId, 0)
                })
                .ToList();

            return Result<List<AvailableAddOnDto>>.Success(availableAddOns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available add-ons for clinic {ClinicId}", clinicId);
            return Result<List<AvailableAddOnDto>>.Failure("Failed to get available add-ons");
        }
    }

    // Old GetAvailableAddOnsAsync removed — replaced by the one above that uses package features

    public async Task<Result<List<SubscriptionAddOnDto>>> GetActiveAddOnsAsync(Guid clinicId)
    {
        try
        {
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (activeSubscription == null)
            {
                return Result<List<SubscriptionAddOnDto>>.Success(new List<SubscriptionAddOnDto>());
            }

            var addOns = await _addOnRepository.GetActiveBySubscriptionIdAsync(activeSubscription.Id);
            var result = addOns.Adapt<List<SubscriptionAddOnDto>>();

            return Result<List<SubscriptionAddOnDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active add-ons for clinic {ClinicId}", clinicId);
            return Result<List<SubscriptionAddOnDto>>.Failure("Failed to get active add-ons");
        }
    }

    public async Task<Result<List<AddOnDto>>> GetClinicAddOnsAsync(Guid clinicId, AddOnStatus? status = null)
    {
        try
        {
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (activeSubscription == null)
                return Result<List<AddOnDto>>.Success(new List<AddOnDto>());

            var addOns = await _addOnRepository.GetBySubscriptionIdAsync(activeSubscription.Id, status);
            var result = addOns.Adapt<List<AddOnDto>>();
            return Result<List<AddOnDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get clinic add-ons for clinic {ClinicId}", clinicId);
            return Result<List<AddOnDto>>.Failure("Failed to get clinic add-ons");
        }
    }

    public async Task<Result<AddOnDto>> CreateAddOnAsync(Guid clinicId, CreateAddOnRequestDto request)
    {
        try
        {
            if (request.EndDate <= request.StartDate)
                return Result<AddOnDto>.Failure("End date must be after start date");

            if (request.EndDate <= _clock.UtcNow)
                return Result<AddOnDto>.Failure("End date must be in the future");

            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (activeSubscription == null)
                return Result<AddOnDto>.Failure("No active subscription found for clinic");

            var feature = await _featureRepository.GetByIdAsync(request.FeatureId);
            if (feature == null)
                return Result<AddOnDto>.Failure("Feature not found");

            var existingAddOn = await _addOnRepository.GetBySubscriptionAndFeatureAsync(activeSubscription.Id, request.FeatureId);

            SubscriptionAddOn addOn;
            if (existingAddOn != null)
            {
                existingAddOn.Quantity = request.Limit ?? 1;
                existingAddOn.CalculatedPrice = request.Price;
                existingAddOn.Status = AddOnStatus.Active;
                existingAddOn.StartDate = request.StartDate;
                existingAddOn.EndDate = request.EndDate;
                existingAddOn.CancelledAt = null;
                existingAddOn.UpdatedAt = _clock.UtcNow;
                addOn = existingAddOn;
            }
            else
            {
                addOn = new SubscriptionAddOn
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = activeSubscription.Id,
                    FeatureId = request.FeatureId,
                    Quantity = request.Limit ?? 1,
                    CalculatedPrice = request.Price,
                    Status = AddOnStatus.Active,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedAt = _clock.UtcNow
                };
                await _addOnRepository.AddAsync(addOn);
            }

            await _unitOfWork.SaveChangesAsync();

            await RecalculateClinicLimitsAsync(clinicId);

            _logger.LogInformation("Created add-on {AddOnId} for clinic {ClinicId}", addOn.Id, clinicId);

            var dto = new AddOnDto
            {
                Id = addOn.Id,
                FeatureId = addOn.FeatureId,
                FeatureName = feature.Name,
                Limit = addOn.Quantity,
                Price = addOn.CalculatedPrice,
                PaymentType = request.PaymentType,
                Status = addOn.Status,
                StartDate = addOn.StartDate,
                EndDate = addOn.EndDate
            };

            return Result<AddOnDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create add-on for clinic {ClinicId}", clinicId);
            return Result<AddOnDto>.Failure("Failed to create add-on");
        }
    }

    public async Task<Result> CancelAddOnAsync(Guid clinicId, Guid addOnId)
    {
        try
        {
            var addOn = await _addOnRepository.GetWithSubscriptionAndFeatureAsync(addOnId);
            if (addOn == null)
                return Result.Failure("Add-on not found");

            if (addOn.Subscription == null || addOn.Subscription.ClinicId != clinicId)
                return Result.Failure("Add-on does not belong to this clinic");

            if (addOn.Status != AddOnStatus.Active)
                return Result.Failure("Add-on is not active");

            addOn.Status = AddOnStatus.Cancelled;
            addOn.CancelledAt = _clock.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await RecalculateClinicLimitsAsync(clinicId);

            _logger.LogInformation("Cancelled add-on {AddOnId} for clinic {ClinicId}", addOnId, clinicId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel add-on {AddOnId} for clinic {ClinicId}", addOnId, clinicId);
            return Result.Failure("Failed to cancel add-on");
        }
    }

    public async Task<Result> RequestAddOnAsync(Guid clinicId, Guid featureId, string? notes = null)
    {
        try
        {
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (activeSubscription == null)
                return Result.Failure("No active subscription found for clinic");

            var feature = await _featureRepository.GetByIdAsync(featureId);
            if (feature == null)
                return Result.Failure("Feature not found");

            var existingAddOns = await _addOnRepository.GetActiveBySubscriptionIdAsync(activeSubscription.Id);
            if (existingAddOns.Any(a => a.FeatureId == featureId))
                return Result.Failure("This feature is already an active add-on");

            var addOn = new SubscriptionAddOn
            {
                Id = Guid.NewGuid(),
                SubscriptionId = activeSubscription.Id,
                FeatureId = featureId,
                Quantity = 1,
                CalculatedPrice = 0,
                Status = AddOnStatus.Suspended,
                StartDate = _clock.UtcNow,
                EndDate = _clock.UtcNow.AddMonths(1),
                CreatedAt = _clock.UtcNow
            };

            await _addOnRepository.AddAsync(addOn);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Add-on request created for clinic {ClinicId}, feature {FeatureId}", clinicId, featureId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request add-on for clinic {ClinicId}", clinicId);
            return Result.Failure("Failed to request add-on");
        }
    }

    private static int GetMaxQuantityForPricingType(PricingType pricingType)
    {
        return pricingType switch
        {
            PricingType.Fixed => 1,
            PricingType.PerUser => 100,
            PricingType.PerStorageGB => 1000,
            PricingType.PerUnit => 100,
            _ => 100
        };
    }

    public async Task RecalculateClinicLimitsAsync(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null) return;

        var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
        if (subscription == null) return;

        var package = await _packageRepository.GetWithFeaturesAsync(subscription.PackageId);
        if (package == null) return;

        int? basePatientsLimit = null;
        int? baseUsersLimit = null;
        long baseStorageBytes = 0;

        foreach (var pf in package.Features.Where(pf => pf.Feature != null && pf.IsIncluded))
        {
            var limit = pf.Quantity ?? pf.Limit;
            var code = pf.Feature!.Code;
            if (string.Equals(code, FeatureCodes.Patients, StringComparison.OrdinalIgnoreCase))
                basePatientsLimit = limit;
            else if (string.Equals(code, FeatureCodes.Users, StringComparison.OrdinalIgnoreCase))
                baseUsersLimit = limit;
            else if (string.Equals(code, FeatureCodes.Storage, StringComparison.OrdinalIgnoreCase))
                baseStorageBytes = (limit ?? 0) * 1024L * 1024 * 1024;
        }

        var activeAddOns = await _addOnRepository.GetActiveBySubscriptionIdAsync(subscription.Id);

        int addonPatients = 0, addonUsers = 0;
        long addonStorageBytes = 0;

        foreach (var addOn in activeAddOns)
        {
            if (addOn.Feature == null) continue;

            var code = addOn.Feature.Code;
            var qty = addOn.Quantity;
            if (string.Equals(code, FeatureCodes.Patients, StringComparison.OrdinalIgnoreCase))
                addonPatients += qty;
            else if (string.Equals(code, FeatureCodes.Users, StringComparison.OrdinalIgnoreCase))
                addonUsers += qty;
            else if (string.Equals(code, FeatureCodes.Storage, StringComparison.OrdinalIgnoreCase))
                addonStorageBytes += qty * 1024L * 1024 * 1024;
        }

        clinic.PatientsLimit = (basePatientsLimit ?? 0) + addonPatients;
        clinic.UsersLimit = (baseUsersLimit ?? 0) + addonUsers;
        clinic.StorageLimitBytes = baseStorageBytes + addonStorageBytes;

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Recalculated limits for clinic {ClinicId}: patients={Patients}, users={Users}, storage={Storage}GB",
            clinicId, clinic.PatientsLimit, clinic.UsersLimit, clinic.StorageLimitBytes / (1024 * 1024 * 1024));
    }

}
