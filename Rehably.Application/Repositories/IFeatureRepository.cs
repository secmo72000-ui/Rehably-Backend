using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Repositories;

public interface IFeatureRepository : IRepository<Feature>
{
    Task<Feature?> GetByCodeAsync(string code);
    Task<IEnumerable<Feature>> GetActiveFeaturesAsync();
    Task<IEnumerable<Feature>> GetByCategoryAsync(Guid categoryId);
    Task<IEnumerable<Feature>> GetAddOnFeaturesAsync();
    Task<Feature?> GetWithPricingAsync(Guid featureId);
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeFeatureId = null);
    Task<IEnumerable<Feature>> GetFeaturesWithCurrentPricingAsync();
    Task<IEnumerable<Feature>> GetAddOnFeaturesWithPricingAsync();
    Task<Feature?> GetAddOnWithPricingAsync(Guid featureId);
    Task<bool> ExistsAsync(Guid featureId);
    Task<IEnumerable<Guid>> GetActiveCategoryIdsAsync();
}
