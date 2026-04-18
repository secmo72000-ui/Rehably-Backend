using Rehably.Application.Common;
using Rehably.Application.DTOs.ClinicPortal;

namespace Rehably.Application.Services.ClinicPortal;

public interface IPatientService
{
    Task<Result<PagedResult<PatientListDto>>> GetAllAsync(PatientQueryParams query, CancellationToken ct = default);
    Task<Result<PatientDetailDto>> GetByIdAsync(Guid patientId, CancellationToken ct = default);
    Task<Result<PatientDetailDto>> CreateAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<Result<PatientDetailDto>> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid patientId, CancellationToken ct = default);
    Task<Result<PatientDetailDto>> DischargeAsync(Guid patientId, CancellationToken ct = default);
}
