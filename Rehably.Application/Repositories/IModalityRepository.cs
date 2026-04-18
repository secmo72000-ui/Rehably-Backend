using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface IModalityRepository : IRepository<Modality>
{
    Task<IEnumerable<Modality>> GetGlobalModalitiesAsync();
    Task<IEnumerable<Modality>> GetClinicModalitiesAsync(Guid clinicId);
    Task<Modality?> GetWithDetailsAsync(Guid modalityId);
    Task<IEnumerable<Modality>> GetVisibleModalitiesAsync(Guid clinicId, HashSet<Guid> hiddenIds);
    Task<IEnumerable<Modality>> GetClinicOwnedModalitiesAsync(Guid clinicId);
    Task<bool> GlobalItemExistsAsync(Guid globalItemId);
}
