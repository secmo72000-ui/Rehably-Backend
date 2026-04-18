using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class ClinicLibraryOverrideRepository : Repository<ClinicLibraryOverride>, IClinicLibraryOverrideRepository
{
    public ClinicLibraryOverrideRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ClinicLibraryOverride>> GetByClinicIdAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(o => o.ClinicId == clinicId && !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<ClinicLibraryOverride?> GetByClinicAndItemAsync(Guid clinicId, Guid globalItemId, LibraryType libraryType)
    {
        return await _dbSet
            .FirstOrDefaultAsync(o => o.ClinicId == clinicId &&
                                      o.GlobalItemId == globalItemId &&
                                      o.LibraryType == libraryType &&
                                      !o.IsDeleted);
    }

    public async Task<IEnumerable<Guid>> GetHiddenItemIdsAsync(Guid clinicId, LibraryType libraryType)
    {
        return await _dbSet
            .Where(o => o.ClinicId == clinicId &&
                        o.LibraryType == libraryType &&
                        o.IsHidden &&
                        !o.IsDeleted)
            .Select(o => o.GlobalItemId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ClinicLibraryOverride>> GetHiddenItemsAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(o => o.ClinicId == clinicId && o.IsHidden && !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> IsItemHiddenAsync(Guid clinicId, Guid globalItemId, LibraryType libraryType)
    {
        return await _dbSet
            .AnyAsync(o => o.ClinicId == clinicId &&
                           o.GlobalItemId == globalItemId &&
                           o.LibraryType == libraryType &&
                           o.IsHidden &&
                           !o.IsDeleted);
    }

    public async Task<IEnumerable<ClinicLibraryOverride>> GetNonHiddenOverridesAsync(Guid clinicId, LibraryType libraryType)
    {
        return await _dbSet
            .Where(o => o.ClinicId == clinicId &&
                        o.LibraryType == libraryType &&
                        !o.IsHidden &&
                        !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<ClinicLibraryOverride>> GetByClinicAndTypeAsync(Guid clinicId, LibraryType? type)
    {
        var query = _dbSet.Where(o => o.ClinicId == clinicId && !o.IsDeleted);

        if (type.HasValue)
        {
            query = query.Where(o => o.LibraryType == type.Value);
        }

        return await query
            .OrderBy(o => o.LibraryType)
            .ThenBy(o => o.GlobalItemId)
            .ToListAsync();
    }
}

