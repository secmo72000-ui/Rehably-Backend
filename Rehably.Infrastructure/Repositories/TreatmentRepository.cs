using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class TreatmentRepository : Repository<Treatment>, ITreatmentRepository
{
    public TreatmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Treatment>> GetByBodyRegionAsync(Guid bodyRegionCategoryId)
    {
        return await _dbSet
            .Include(t => t.BodyRegionCategory)
            .Where(t => t.BodyRegionCategoryId == bodyRegionCategoryId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Treatment>> GetGlobalTreatmentsAsync()
    {
        return await _dbSet
            .Include(t => t.BodyRegionCategory)
            .Where(t => t.ClinicId == null && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Treatment>> GetClinicTreatmentsAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(t => t.BodyRegionCategory)
            .Where(t => t.ClinicId == clinicId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<Treatment?> GetWithDetailsAsync(Guid treatmentId)
    {
        return await _dbSet
            .Include(t => t.BodyRegionCategory)
            .FirstOrDefaultAsync(t => t.Id == treatmentId && !t.IsDeleted);
    }

    public async Task<IEnumerable<Treatment>> GetVisibleTreatmentsAsync(Guid clinicId, HashSet<Guid> hiddenIds)
    {
        return await _dbSet
            .Include(t => t.BodyRegionCategory)
            .Where(t => !t.IsDeleted)
            .Where(t => (t.ClinicId == null && !hiddenIds.Contains(t.Id)) || t.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Treatment>> GetClinicOwnedTreatmentsAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(t => t.ClinicId == clinicId && !t.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> GlobalItemExistsAsync(Guid globalItemId)
    {
        return await _dbSet
            .AnyAsync(t => t.Id == globalItemId && t.ClinicId == null && !t.IsDeleted);
    }
}
