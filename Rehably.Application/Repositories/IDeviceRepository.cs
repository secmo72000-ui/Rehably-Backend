using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface IDeviceRepository : IRepository<Device>
{
    Task<IEnumerable<Device>> GetGlobalDevicesAsync();
    Task<IEnumerable<Device>> GetClinicDevicesAsync(Guid clinicId);
    Task<Device?> GetWithDetailsAsync(Guid deviceId);
    Task<IEnumerable<Device>> GetVisibleDevicesAsync(Guid clinicId, HashSet<Guid> hiddenIds);
    Task<IEnumerable<Device>> GetClinicOwnedDevicesAsync(Guid clinicId);
    Task<bool> GlobalItemExistsAsync(Guid globalItemId);
}
