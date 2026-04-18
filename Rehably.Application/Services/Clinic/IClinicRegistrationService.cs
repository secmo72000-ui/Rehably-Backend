using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;

namespace Rehably.Application.Services.Clinic;

public interface IClinicRegistrationService
{
    Task<Result<RegisterClinicResponse>> RegisterClinicAsync(RegisterClinicRequest request);
    Task<Result<ClinicResponse>> CreateClinicAsync(CreateClinicRequest request);
}
