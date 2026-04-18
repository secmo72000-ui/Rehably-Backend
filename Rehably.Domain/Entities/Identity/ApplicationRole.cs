using Microsoft.AspNetCore.Identity;

namespace Rehably.Domain.Entities.Identity;

public class ApplicationRole : IdentityRole
{
    public Guid? TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsCustom { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
