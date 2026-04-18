using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface IAssessmentRepository : IRepository<Assessment>
{
    Task<IEnumerable<Assessment>> GetGlobalAssessmentsAsync();
    Task<IEnumerable<Assessment>> GetClinicAssessmentsAsync(Guid clinicId);
    Task<Assessment?> GetWithDetailsAsync(Guid assessmentId);
    Task<IEnumerable<Assessment>> GetVisibleAssessmentsAsync(Guid clinicId, HashSet<Guid> hiddenIds);
    Task<IEnumerable<Assessment>> GetClinicOwnedAssessmentsAsync(Guid clinicId);
    Task<bool> GlobalItemExistsAsync(Guid globalItemId);
}
