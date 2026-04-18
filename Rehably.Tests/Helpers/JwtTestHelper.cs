using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Rehably.Tests.Helpers;

public static class JwtTestHelper
{
    public static ClaimsPrincipal CreateClaimsPrincipal(
        string userId,
        int tenantId,
        bool mustChangePassword = false,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("TenantId", tenantId.ToString()),
            new Claim("mustChangePassword", mustChangePassword.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    public static bool GetMustChangePasswordClaim(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("mustChangePassword")?.Value;
        return claim == "true";
    }
}
