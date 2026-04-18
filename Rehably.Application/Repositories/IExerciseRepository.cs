using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface IExerciseRepository : IRepository<Exercise>
{
    Task<IEnumerable<Exercise>> GetByBodyRegionAsync(Guid bodyRegionCategoryId);
    Task<IEnumerable<Exercise>> GetGlobalExercisesAsync();
    Task<IEnumerable<Exercise>> GetClinicExercisesAsync(Guid clinicId);
    Task<Exercise?> GetWithDetailsAsync(Guid exerciseId);
    Task<IEnumerable<Exercise>> GetVisibleExercisesAsync(Guid clinicId, HashSet<Guid> hiddenIds);
    Task<IEnumerable<Exercise>> GetClinicOwnedExercisesAsync(Guid clinicId);
    Task<bool> GlobalItemExistsAsync(Guid globalItemId);
}
