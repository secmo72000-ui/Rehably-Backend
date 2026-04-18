using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface IBodyRegionCategoryRepository : IRepository<BodyRegionCategory>
{
    Task<IEnumerable<BodyRegionCategory>> GetActiveAsync();
    Task<BodyRegionCategory?> GetWithItemsAsync(Guid categoryId);
}
