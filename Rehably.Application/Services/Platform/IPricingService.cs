using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.Pricing;

namespace Rehably.Application.Services.Platform;

public interface IPricingService
{
    Task<Result<decimal>> CalculatePackagePriceAsync(Guid packageId, PricingCalculationRequestDto request);
    Task<Result<PackagePricingBreakdownDto>> GetPricingBreakdownAsync(Guid packageId, PricingCalculationRequestDto request);
    Task<Result<PackageSnapshotDto>> CreatePackageSnapshotAsync(Guid packageId);
    Task<Result<PackageSnapshotDto>> CalculateModifiedPackagePriceAsync(Guid packageId, List<PackageFeatureDto> modifiedFeatures);
}
