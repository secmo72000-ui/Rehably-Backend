using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;

namespace Rehably.Application.Services.Platform;

public interface IPackageService
{
    Task<Result<PackageDto>> GetPackageByIdAsync(Guid id);
    Task<Result<List<PackageDto>>> GetPackagesAsync();
    Task<Result<List<PublicPackageDto>>> GetPublicPackagesAsync();
    Task<Result<PackageDetailDto>> GetPackageWithDetailsAsync(Guid id);
    Task<Result<PackageDto>> CreatePackageAsync(CreatePackageRequestDto request);
    Task<Result<PackageDto>> UpdatePackageAsync(Guid id, UpdatePackageRequestDto request);
    Task<Result> ActivatePackageAsync(Guid id);
    Task<Result> ArchivePackageAsync(Guid id);
    Task<Result<PackageDetailDto>> RecalculatePackagePriceAsync(Guid id);
    Task<Result> DeletePackageAsync(Guid id);
}
