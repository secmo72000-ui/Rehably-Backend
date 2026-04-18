using Rehably.Domain.Entities.Library;

namespace Rehably.Application.Repositories;

public interface ITreatmentStageRepository : IRepository<TreatmentStage>
{
    Task<TreatmentStage?> GetWithDetailsAsync(Guid id);
}
