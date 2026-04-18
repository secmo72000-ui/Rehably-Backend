using Rehably.Domain.Entities.Platform;

namespace Rehably.Application.Repositories;

public interface IFeatureCategoryRepository : IRepository<FeatureCategory>
{
    Task<FeatureCategory?> GetByCodeAsync(string code);
    Task<IEnumerable<FeatureCategory>> GetActiveCategoriesAsync();
    Task<IEnumerable<FeatureCategory>> GetRootCategoriesAsync();
    Task<IEnumerable<FeatureCategory>> GetSubCategoriesAsync(Guid parentCategoryId);
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeCategoryId = null);
    Task<bool> HasChildrenAsync(Guid categoryId);
    Task<bool> HasFeaturesAsync(Guid categoryId);
    Task<IEnumerable<FeatureCategory>> GetCategoryTreeAsync();
    Task<IEnumerable<Guid>> GetActiveCategoryIdsAsync();
    Task<bool> IsDescendantAsync(Guid categoryId, Guid potentialParentId);
}
