using Microsoft.AspNetCore.Authorization;

namespace Rehably.API.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    private const string POLICY_PREFIX = "Permission:";

    /// <summary>
    /// Require a specific permission using typed Permission object.
    /// Usage: [RequirePermission(Permissions.Patients.View)]
    /// </summary>
    public RequirePermissionAttribute(Permission permission)
    {
        Policy = POLICY_PREFIX + permission.ToString();
    }

    /// <summary>
    /// Require a specific permission using string.
    /// Usage: [RequirePermission("patients.view")]
    /// </summary>
    public RequirePermissionAttribute(string permission)
    {
        Policy = POLICY_PREFIX + permission;
    }
}
