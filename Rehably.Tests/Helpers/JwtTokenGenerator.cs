using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Rehably.Tests.Helpers;

/// <summary>
/// Centralized JWT token generator for integration and security tests.
/// Uses the same signing key, issuer, and audience as the application.
/// </summary>
public static class JwtTokenGenerator
{
    private const string SigningKey = "rehablyMulti-Tanatet-Projec-saaaas-141515r234er434134r13r4r123r";
    private const string Issuer = "rehably";
    private const string Audience = "rehably-api";

    /// <summary>
    /// Generates a JWT token with full control over all claims.
    /// </summary>
    public static string GenerateToken(
        string? userId = null,
        string? email = null,
        Guid? tenantId = null,
        string[]? roles = null,
        string[]? permissions = null,
        bool mustChangePassword = false,
        DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, email ?? "test@test.com"),
            new("mustChangePassword", mustChangePassword.ToString().ToLower())
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("TenantId", tenantId.Value.ToString()));

        if (roles is not null)
        {
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (permissions is not null)
        {
            foreach (var permission in permissions)
                claims.Add(new Claim("Permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a token for a PlatformAdmin with wildcard permissions and no tenant.
    /// </summary>
    public static string PlatformAdminToken()
    {
        return GenerateToken(
            userId: "test-platform-admin",
            email: "admin@rehably.com",
            roles: ["PlatformAdmin"],
            permissions: ["*.*"]);
    }

    /// <summary>
    /// Generates a token for a ClinicOwner bound to the specified tenant.
    /// </summary>
    public static string ClinicOwnerToken(Guid tenantId)
    {
        return GenerateToken(
            userId: "test-clinic-owner",
            email: "owner@clinic.com",
            tenantId: tenantId,
            roles: ["ClinicOwner"],
            permissions: ["*.*"]);
    }

    /// <summary>
    /// Generates a token for a tenant user with configurable permissions.
    /// </summary>
    public static string TenantUserToken(Guid tenantId, params string[] permissions)
    {
        return GenerateToken(
            userId: "test-tenant-user",
            email: "user@clinic.com",
            tenantId: tenantId,
            roles: ["User"],
            permissions: permissions);
    }

    /// <summary>
    /// Generates an expired token for testing token expiry scenarios.
    /// </summary>
    public static string ExpiredToken()
    {
        return GenerateToken(
            userId: "test-expired-user",
            email: "expired@test.com",
            tenantId: Guid.NewGuid(),
            roles: ["User"],
            permissions: ["*.*"],
            expires: DateTime.UtcNow.AddHours(-1));
    }

    /// <summary>
    /// Generates a token with no TenantId claim for testing missing-tenant scenarios.
    /// </summary>
    public static string TokenWithNoTenant()
    {
        return GenerateToken(
            userId: "test-no-tenant-user",
            email: "notenant@test.com",
            tenantId: null,
            roles: ["User"],
            permissions: ["*.*"]);
    }
}
