using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface ITreatmentRepository : IRepository<Treatment>
{
    Task<IEnumerable<Treatment>> GetByBodyRegionAsync(Guid bodyRegionCategoryId);
    Task<IEnumerable<Treatment>> GetGlobalTreatmentsAsync();
    Task<IEnumerable<Treatment>> GetClinicTreatmentsAsync(Guid clinicId);
    Task<Treatment?> GetWithDetailsAsync(Guid treatmentId);
    Task<IEnumerable<Treatment>> GetVisibleTreatmentsAsync(Guid clinicId, HashSet<Guid> hiddenIds);
    Task<IEnumerable<Treatment>> GetClinicOwnedTreatmentsAsync(Guid clinicId);
    Task<bool> GlobalItemExistsAsync(Guid globalItemId);
}
