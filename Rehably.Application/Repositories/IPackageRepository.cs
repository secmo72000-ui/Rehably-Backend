using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Repositories;

public interface IPackageRepository : IRepository<Package>
{
    Task<IEnumerable<Package>> GetActivePackagesAsync();
    Task<Package?> GetWithFeaturesAsync(Guid packageId);
    Task<Package?> GetByCodeAsync(string code);
    Task<IEnumerable<Package>> GetPublicPackagesAsync();
    Task<IEnumerable<Package>> GetByStatusAsync(PackageStatus status);
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludePackageId = null);
    Task<Package?> GetWithPricingHistoryAsync(Guid packageId);
    Task<List<Guid>> GetIncludedFeatureIdsAsync(Guid packageId);
    Task<Package?> GetWithFeaturesAndPricingAsync(Guid packageId, bool activeOnly = false);
    Task<IEnumerable<Package>> GetAllForAdminAsync();
    Task<Package?> GetForEditAsync(Guid packageId);
    Task ClearFeaturesAsync(Guid packageId);
    Task<bool> HasAnySubscriptionsAsync(Guid packageId);
}

public interface IPackageFeatureRepository : IRepository<PackageFeature>
{
    Task<bool> IsFeatureInActivePackagesAsync(Guid featureId);
}
