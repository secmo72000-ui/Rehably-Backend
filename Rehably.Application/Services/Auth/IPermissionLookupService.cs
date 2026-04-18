namespace Rehably.Application.Services.Auth;

public interface IPermissionLookupService
{
    Task<HashSet<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames);
}
