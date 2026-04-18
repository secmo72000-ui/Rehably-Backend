using Rehably.Application.Common;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.DTOs.Subscription;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Platform;

public interface IAddOnService
{
    Task<Result<List<FeatureDto>>> GetAddOnFeaturesAsync();
    Task<Result> SetFeatureAddOnStatusAsync(Guid featureId, bool isAddOn);
    Task<Result<List<AvailableAddOnDto>>> GetAvailableAddOnsAsync(Guid clinicId);
    Task<Result<List<SubscriptionAddOnDto>>> GetActiveAddOnsAsync(Guid clinicId);
    Task<Result<List<AddOnDto>>> GetClinicAddOnsAsync(Guid clinicId, AddOnStatus? status = null);
    Task<Result<AddOnDto>> CreateAddOnAsync(Guid clinicId, CreateAddOnRequestDto request);
    Task<Result> CancelAddOnAsync(Guid clinicId, Guid addOnId);
    Task<Result> RequestAddOnAsync(Guid clinicId, Guid featureId, string? notes = null);
    Task RecalculateClinicLimitsAsync(Guid clinicId);
}
