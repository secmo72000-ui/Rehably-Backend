using Microsoft.AspNetCore.Authorization;

namespace Rehably.API.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    private const string POLICY_PREFIX ="Permission";
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
