using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Clinic;

public interface ITenantResolutionService
{
    Task<(ClinicStatus Status, bool Exists)?> ResolveClinicAsync(Guid tenantId);
}
