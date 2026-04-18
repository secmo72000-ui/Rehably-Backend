using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;

namespace Rehably.API.Controllers.Clinic;

[Authorize]
[Route("api/clinic/insurance")]
public class ClinicInsuranceController : BaseController
{
    private readonly IInsuranceService _insurance;
    public ClinicInsuranceController(IInsuranceService insurance) => _insurance = insurance;

    private Guid ClinicId => TenantId.GetValueOrDefault();

    [HttpGet("providers/global")]
    public async Task<IActionResult> GetGlobalProviders([FromQuery] InsuranceQueryParams query)
        => Ok(await _insurance.GetGlobalProvidersAsync(query));

    [HttpGet("providers")]
    public async Task<IActionResult> GetClinicProviders()
        => Ok(await _insurance.GetClinicProvidersAsync(ClinicId));

    [HttpGet("providers/{id:guid}")]
    public async Task<IActionResult> GetClinicProvider(Guid id)
    {
        var result = await _insurance.GetClinicProviderByIdAsync(ClinicId, id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("providers")]
    public async Task<IActionResult> ActivateProvider([FromBody] ActivateInsuranceProviderRequest request)
        => Ok(await _insurance.ActivateProviderAsync(ClinicId, request));

    [HttpPut("providers/{id:guid}")]
    public async Task<IActionResult> UpdateClinicProvider(Guid id, [FromBody] UpdateClinicInsuranceProviderRequest request)
        => Ok(await _insurance.UpdateClinicProviderAsync(ClinicId, id, request));

    [HttpDelete("providers/{id:guid}")]
    public async Task<IActionResult> DeactivateClinicProvider(Guid id)
    {
        await _insurance.DeactivateClinicProviderAsync(ClinicId, id);
        return NoContent();
    }

    [HttpGet("patients/{patientId:guid}")]
    public async Task<IActionResult> GetPatientInsurances(Guid patientId)
        => Ok(await _insurance.GetPatientInsurancesAsync(ClinicId, patientId));

    [HttpPost("patients")]
    public async Task<IActionResult> AddPatientInsurance([FromBody] AddPatientInsuranceRequest request)
        => Ok(await _insurance.AddPatientInsuranceAsync(ClinicId, request));

    [HttpPut("patients/{id:guid}")]
    public async Task<IActionResult> UpdatePatientInsurance(Guid id, [FromBody] UpdatePatientInsuranceRequest request)
        => Ok(await _insurance.UpdatePatientInsuranceAsync(ClinicId, id, request));

    [HttpDelete("patients/{id:guid}")]
    public async Task<IActionResult> DeletePatientInsurance(Guid id)
    {
        await _insurance.DeletePatientInsuranceAsync(ClinicId, id);
        return NoContent();
    }

    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims([FromQuery] ClaimQueryParams query)
        => Ok(await _insurance.GetClaimsAsync(ClinicId, query));

    [HttpGet("claims/{id:guid}")]
    public async Task<IActionResult> GetClaim(Guid id)
    {
        var result = await _insurance.GetClaimByIdAsync(ClinicId, id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("claims")]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimRequest request)
        => Ok(await _insurance.SubmitClaimAsync(ClinicId, request));

    [HttpPut("claims/{id:guid}")]
    public async Task<IActionResult> UpdateClaim(Guid id, [FromBody] UpdateClaimRequest request)
        => Ok(await _insurance.UpdateClaimAsync(ClinicId, id, request));
}
