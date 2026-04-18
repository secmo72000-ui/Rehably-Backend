using Microsoft.EntityFrameworkCore;
using Rehably.Application.Services.Auth;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Auth;

public class PermissionLookupService : IPermissionLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public PermissionLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HashSet<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames)
    {
        var roleNameList = roleNames.ToList();
        if (roleNameList.Count == 0)
            return new HashSet<string>();

        var permissions = await _dbContext.RoleClaims
            .Join(
                _dbContext.Roles,
                rc => rc.RoleId,
                r => r.Id,
                (rc, r) => new { r.Name, rc.ClaimType, rc.ClaimValue })
            .Where(x => roleNameList.Contains(x.Name) && x.ClaimType == "Permission" && x.ClaimValue != null)
            .Select(x => x.ClaimValue!)
            .Distinct()
            .ToListAsync();

        return permissions.ToHashSet();
    }
}
