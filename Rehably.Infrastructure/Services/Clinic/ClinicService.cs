using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicService : IClinicService
{
    private readonly IClinicCrudService _crudService;
    private readonly IClinicRegistrationService _registrationService;
    private readonly IClinicQueryService _queryService;
    private readonly IClinicBillingService _billingService;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(
        IClinicCrudService crudService,
        IClinicRegistrationService registrationService,
        IClinicQueryService queryService,
        IClinicBillingService billingService,
        ILogger<ClinicService> logger)
    {
        _crudService = crudService;
        _registrationService = registrationService;
        _queryService = queryService;
        _billingService = billingService;
        _logger = logger;
    }

    public Task<Result<RegisterClinicResponse>> RegisterClinicAsync(RegisterClinicRequest request)
        => _registrationService.RegisterClinicAsync(request);

    public Task<Result<ClinicResponse>> CreateClinicAsync(CreateClinicRequest request)
        => _registrationService.CreateClinicAsync(request);

    public Task<Result<ClinicResponse>> GetClinicByIdAsync(Guid id)
        => _crudService.GetClinicByIdAsync(id);

    public Task<Result<ClinicResponse>> UpdateClinicAsync(Guid id, UpdateClinicRequest request)
        => _crudService.UpdateClinicAsync(id, request);

    public Task<Result> DeleteClinicAsync(Guid id)
        => _crudService.DeleteClinicAsync(id);

    public Task<Result<ClinicResponse>> GetMyClinicAsync()
        => _crudService.GetMyClinicAsync();

    public Task<Result<ClinicResponse>> UpdateMyClinicAsync(UpdateClinicRequest request)
        => _crudService.UpdateMyClinicAsync(request);

    public Task<Result<PagedResult<ClinicResponse>>> GetAllClinicsAsync(int page = 1, int pageSize = 20)
        => _queryService.GetAllClinicsAsync(page, pageSize);

    public Task<Result<PagedResult<ClinicResponse>>> SearchClinicsAsync(GetClinicsQuery query)
        => _queryService.SearchClinicsAsync(query);

    public Task<Result<List<ClinicResponse>>> GetPendingClinicsAsync()
        => _queryService.GetPendingClinicsAsync();

    public Task<bool> CanAddPatientAsync(Guid clinicId)
        => _queryService.CanAddPatientAsync(clinicId);

    public Task<bool> CanAddUserAsync(Guid clinicId)
        => _queryService.CanAddUserAsync(clinicId);

    public Task<bool> CanUploadStorageAsync(Guid clinicId, long bytes)
        => _queryService.CanUploadStorageAsync(clinicId, bytes);

    public void ValidateTenantAccess(Guid requestedClinicId)
        => _queryService.ValidateTenantAccess(requestedClinicId);

    public Task<Result> UpgradeSubscriptionAsync(Guid clinicId, Guid newPlanId)
        => _billingService.UpgradeSubscriptionAsync(clinicId, newPlanId);

    public Task<Result> SuspendClinicAsync(Guid clinicId)
        => _billingService.SuspendClinicAsync(clinicId);

    public Task<Result> ActivateClinicAsync(Guid clinicId)
        => _billingService.ActivateClinicAsync(clinicId);

    public Task<Result> BanClinicAsync(Guid clinicId, string reason, string adminUserId)
        => _billingService.BanClinicAsync(clinicId, reason, adminUserId);

    public Task<Result> UnbanClinicAsync(Guid clinicId, string? reason, string adminUserId)
        => _billingService.UnbanClinicAsync(clinicId, reason, adminUserId);
}
