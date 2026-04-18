using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class ExerciseRepository : Repository<Exercise>, IExerciseRepository
{
    public ExerciseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Exercise>> GetByBodyRegionAsync(Guid bodyRegionCategoryId)
    {
        return await _dbSet
            .Include(e => e.BodyRegionCategory)
            .Where(e => e.BodyRegionCategoryId == bodyRegionCategoryId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Exercise>> GetGlobalExercisesAsync()
    {
        return await _dbSet
            .Include(e => e.BodyRegionCategory)
            .Where(e => e.ClinicId == null && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Exercise>> GetClinicExercisesAsync(Guid clinicId)
    {
        return await _dbSet
            .Include(e => e.BodyRegionCategory)
            .Where(e => e.ClinicId == clinicId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<Exercise?> GetWithDetailsAsync(Guid exerciseId)
    {
        return await _dbSet
            .Include(e => e.BodyRegionCategory)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);
    }

    public async Task<IEnumerable<Exercise>> GetVisibleExercisesAsync(Guid clinicId, HashSet<Guid> hiddenIds)
    {
        return await _dbSet
            .Include(e => e.BodyRegionCategory)
            .Where(e => !e.IsDeleted)
            .Where(e => (e.ClinicId == null && !hiddenIds.Contains(e.Id)) || e.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Exercise>> GetClinicOwnedExercisesAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(e => e.ClinicId == clinicId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> GlobalItemExistsAsync(Guid globalItemId)
    {
        return await _dbSet
            .AnyAsync(e => e.Id == globalItemId && e.ClinicId == null && !e.IsDeleted);
    }
}
