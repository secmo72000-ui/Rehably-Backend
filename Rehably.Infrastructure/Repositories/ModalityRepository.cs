using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class ModalityRepository : Repository<Modality>, IModalityRepository
{
    public ModalityRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Modality>> GetGlobalModalitiesAsync()
    {
        return await _dbSet
            .Where(m => m.ClinicId == null && !m.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Modality>> GetClinicModalitiesAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(m => m.ClinicId == clinicId && !m.IsDeleted)
            .ToListAsync();
    }

    public async Task<Modality?> GetWithDetailsAsync(Guid modalityId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.Id == modalityId && !m.IsDeleted);
    }

    public async Task<IEnumerable<Modality>> GetVisibleModalitiesAsync(Guid clinicId, HashSet<Guid> hiddenIds)
    {
        return await _dbSet
            .Where(m => !m.IsDeleted)
            .Where(m => (m.ClinicId == null && !hiddenIds.Contains(m.Id)) || m.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Modality>> GetClinicOwnedModalitiesAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(m => m.ClinicId == clinicId && !m.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> GlobalItemExistsAsync(Guid globalItemId)
    {
        return await _dbSet
            .AnyAsync(m => m.Id == globalItemId && m.ClinicId == null && !m.IsDeleted);
    }
}
