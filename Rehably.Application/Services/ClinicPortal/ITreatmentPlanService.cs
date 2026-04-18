using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface ITreatmentPlanService
{
    Task<Result<PagedResult<TreatmentPlanDto>>> GetAllAsync(TreatmentPlanQueryParams query, CancellationToken ct = default);
    Task<Result<TreatmentPlanDetailDto>> GetByIdAsync(Guid planId, CancellationToken ct = default);
    Task<Result<List<TreatmentPlanDto>>> GetByPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<Result<TreatmentPlanDetailDto>> CreateAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default);
    Task<Result<TreatmentPlanDetailDto>> UpdateAsync(Guid planId, UpdateTreatmentPlanRequest request, CancellationToken ct = default);
    Task<Result<TreatmentPlanDetailDto>> ActivateAsync(Guid planId, CancellationToken ct = default);
    Task<Result<TreatmentPlanDetailDto>> CompleteAsync(Guid planId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid planId, CancellationToken ct = default);

    // Sessions
    Task<Result<List<SessionDto>>> GetSessionsAsync(Guid planId, CancellationToken ct = default);
    Task<Result<SessionDto>> AddSessionAsync(Guid planId, CreateSessionRequest request, CancellationToken ct = default);
    Task<Result<SessionDto>> UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request, CancellationToken ct = default);
    Task<Result<SessionDto>> CompleteSessionAsync(Guid sessionId, CompleteSessionRequest request, CancellationToken ct = default);
}
