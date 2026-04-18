using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Library;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Repositories;

public class DeviceRepository : Repository<Device>, IDeviceRepository
{
    public DeviceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Device>> GetGlobalDevicesAsync()
    {
        return await _dbSet
            .Where(d => d.ClinicId == null && !d.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Device>> GetClinicDevicesAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(d => d.ClinicId == clinicId && !d.IsDeleted)
            .ToListAsync();
    }

    public async Task<Device?> GetWithDetailsAsync(Guid deviceId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);
    }

    public async Task<IEnumerable<Device>> GetVisibleDevicesAsync(Guid clinicId, HashSet<Guid> hiddenIds)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted)
            .Where(d => (d.ClinicId == null && !hiddenIds.Contains(d.Id)) || d.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Device>> GetClinicOwnedDevicesAsync(Guid clinicId)
    {
        return await _dbSet
            .Where(d => d.ClinicId == clinicId && !d.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> GlobalItemExistsAsync(Guid globalItemId)
    {
        return await _dbSet
            .AnyAsync(d => d.Id == globalItemId && d.ClinicId == null && !d.IsDeleted);
    }
}
