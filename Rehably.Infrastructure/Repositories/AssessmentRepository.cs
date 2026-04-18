using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class AssessmentRepository : Repository<Assessment>, IAssessmentRepository
{
    public AssessmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Assessment>> GetGlobalAssessmentsAsync()
    {
        return await _dbSet
            .Where(a => a.ClinicId == null && !a.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assessment>> GetClinicAssessmentsAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(a => a.ClinicId == clinicId && !a.IsDeleted)
            .ToListAsync();
    }

    public async Task<Assessment?> GetWithDetailsAsync(Guid assessmentId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted);
    }

    public async Task<IEnumerable<Assessment>> GetVisibleAssessmentsAsync(Guid clinicId, HashSet<Guid> hiddenIds)
    {
        return await _dbSet
            .Where(a => !a.IsDeleted)
            .Where(a => (a.ClinicId == null && !hiddenIds.Contains(a.Id)) || a.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assessment>> GetClinicOwnedAssessmentsAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(a => a.ClinicId == clinicId && !a.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> GlobalItemExistsAsync(Guid globalItemId)
    {
        return await _dbSet
            .AnyAsync(a => a.Id == globalItemId && a.ClinicId == null && !a.IsDeleted);
    }
}
