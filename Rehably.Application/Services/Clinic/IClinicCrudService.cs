using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Services.Clinic;

public interface IClinicCrudService
{
    Task<Result<ClinicResponse>> GetClinicByIdAsync(Guid id);
    Task<Result<ClinicResponse>> UpdateClinicAsync(Guid id, UpdateClinicRequest request);
    Task<Result> DeleteClinicAsync(Guid id);
    Task<Result<ClinicResponse>> GetMyClinicAsync();
    Task<Result<ClinicResponse>> UpdateMyClinicAsync(UpdateClinicRequest request);
}
