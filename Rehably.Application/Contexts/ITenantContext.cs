namespace Rehably.Application.Contexts;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? UserId { get; }
    void SetTenant(Guid tenantId);
    void SetUser(string userId);
    Guid GetCurrentTenantId();
}
