using Microsoft.AspNetCore.Identity;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? TenantId { get; set; }
    public Guid? ClinicId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public RoleType RoleType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ResetTokenSelector { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
