using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;

namespace Rehably.Application.Services.Billing;

public interface IInsuranceService
{
    // Global provider registry
    Task<PagedResult<InsuranceProviderDto>> GetGlobalProvidersAsync(InsuranceQueryParams query);

    // Clinic-activated providers
    Task<List<ClinicInsuranceProviderDto>> GetClinicProvidersAsync(Guid clinicId);
    Task<ClinicInsuranceProviderDto?> GetClinicProviderByIdAsync(Guid clinicId, Guid id);
    Task<ClinicInsuranceProviderDto> ActivateProviderAsync(Guid clinicId, ActivateInsuranceProviderRequest request);
    Task<ClinicInsuranceProviderDto> UpdateClinicProviderAsync(Guid clinicId, Guid id, UpdateClinicInsuranceProviderRequest request);
    Task DeactivateClinicProviderAsync(Guid clinicId, Guid id);

    // Patient insurance policies
    Task<List<PatientInsuranceDto>> GetPatientInsurancesAsync(Guid clinicId, Guid patientId);
    Task<PatientInsuranceDto> AddPatientInsuranceAsync(Guid clinicId, AddPatientInsuranceRequest request);
    Task<PatientInsuranceDto> UpdatePatientInsuranceAsync(Guid clinicId, Guid id, UpdatePatientInsuranceRequest request);
    Task DeletePatientInsuranceAsync(Guid clinicId, Guid id);

    // Claims
    Task<PagedResult<InsuranceClaimDto>> GetClaimsAsync(Guid clinicId, ClaimQueryParams query);
    Task<InsuranceClaimDto?> GetClaimByIdAsync(Guid clinicId, Guid id);
    Task<InsuranceClaimDto> SubmitClaimAsync(Guid clinicId, SubmitClaimRequest request);
    Task<InsuranceClaimDto> UpdateClaimAsync(Guid clinicId, Guid id, UpdateClaimRequest request);
}
