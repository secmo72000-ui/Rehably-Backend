namespace Rehably.Domain.Entities.Base;

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
