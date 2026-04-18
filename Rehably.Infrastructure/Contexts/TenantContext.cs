using Rehably.Application.Contexts;

namespace Rehably.Infrastructure.Contexts;

public class TenantContext : ITenantContext
{
    private Guid? _tenantId;
    private string? _userId;

    public Guid? TenantId => _tenantId;
    public string? UserId => _userId;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public void SetUser(string userId)
    {
        _userId = userId;
    }

    public Guid GetCurrentTenantId()
    {
        if (!_tenantId.HasValue)
        {
            throw new InvalidOperationException("No tenant context available. User is not associated with a tenant.");
        }

        return _tenantId.Value;
    }
}
