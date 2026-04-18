using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class TreatmentStageRepository : Repository<TreatmentStage>, ITreatmentStageRepository
{
    public TreatmentStageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TreatmentStage?> GetWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(s => s.BodyRegion)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }
}
