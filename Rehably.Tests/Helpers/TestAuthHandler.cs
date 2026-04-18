using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rehably.Tests.Helpers;

/// <summary>
/// Test authentication handler that always authenticates as a PlatformAdmin
/// with wildcard permissions. Use for admin-level integration tests.
/// </summary>
public class TestAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-admin-id"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "PlatformAdmin"),
            new Claim("Permission", "*.*"),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Test authentication handler that authenticates as a tenant user with configurable
/// tenant ID and permissions. Set static properties before each test.
/// </summary>
public class TestTenantAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The tenant ID to include in the authenticated identity. Set before test execution.
    /// </summary>
    public static Guid TenantId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The permissions to include in the authenticated identity. Set before test execution.
    /// </summary>
    public static string[] Permissions { get; set; } = ["*.*"];

    /// <summary>
    /// The role to include in the authenticated identity. Defaults to "ClinicOwner".
    /// </summary>
    public static string Role { get; set; } = "ClinicOwner";

    /// <summary>
    /// The user ID to include in the authenticated identity.
    /// </summary>
    public static string UserId { get; set; } = "test-tenant-user-id";

    public TestTenantAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new(ClaimTypes.Email, "tenant@test.com"),
            new(ClaimTypes.Role, Role),
            new("TenantId", TenantId.ToString()),
        };

        foreach (var permission in Permissions)
            claims.Add(new Claim("Permission", permission));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Resets all static configuration to defaults. Call in test cleanup.
    /// </summary>
    public static void Reset()
    {
        TenantId = Guid.NewGuid();
        Permissions = ["*.*"];
        Role = "ClinicOwner";
        UserId = "test-tenant-user-id";
    }
}

/// <summary>
/// Test authentication handler that always returns no result,
/// simulating an unauthenticated (anonymous) request for testing 401 scenarios.
/// </summary>
public class TestAnonymousAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAnonymousAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
