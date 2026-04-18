using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class FeatureCategoryRepository : Repository<FeatureCategory>, IFeatureCategoryRepository
{
    public FeatureCategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FeatureCategory?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);
    }

    public async Task<IEnumerable<FeatureCategory>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<FeatureCategory>> GetRootCategoriesAsync()
    {
        return await _dbSet
            .Where(c => !c.IsDeleted && c.ParentCategoryId == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<FeatureCategory>> GetSubCategoriesAsync(Guid parentCategoryId)
    {
        return await _dbSet
            .Where(c => !c.IsDeleted && c.ParentCategoryId == parentCategoryId)
            .ToListAsync();
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeCategoryId = null)
    {
        var query = _dbSet.Where(c => c.Code == code && !c.IsDeleted);
        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }
        return !await query.AnyAsync();
    }

    public async Task<bool> HasChildrenAsync(Guid categoryId)
    {
        return await _dbSet.AnyAsync(c => c.ParentCategoryId == categoryId && !c.IsDeleted);
    }

    public async Task<bool> HasFeaturesAsync(Guid categoryId)
    {
        return await _context.Features.AnyAsync(f => f.CategoryId == categoryId && !f.IsDeleted);
    }

    public async Task<IEnumerable<FeatureCategory>> GetCategoryTreeAsync()
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetActiveCategoryIdsAsync()
    {
        return await _dbSet
            .Where(c => !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();
    }

    public async Task<bool> IsDescendantAsync(Guid categoryId, Guid potentialParentId)
    {
        var allCategories = await _dbSet
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.ParentCategoryId })
            .ToListAsync();

        var lookup = allCategories.ToDictionary(c => c.Id, c => c.ParentCategoryId);

        var currentId = (Guid?)potentialParentId;
        var visited = new HashSet<Guid>();

        while (currentId.HasValue && lookup.TryGetValue(currentId.Value, out var parentId))
        {
            if (!visited.Add(currentId.Value))
                break;

            if (parentId == categoryId)
                return true;

            currentId = parentId;
        }

        return false;
    }
}
