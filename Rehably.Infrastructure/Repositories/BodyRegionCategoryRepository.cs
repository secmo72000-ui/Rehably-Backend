using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class BodyRegionCategoryRepository : Repository<BodyRegionCategory>, IBodyRegionCategoryRepository
{
    public BodyRegionCategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BodyRegionCategory>> GetActiveAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task<BodyRegionCategory?> GetWithItemsAsync(Guid categoryId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);
    }
}
