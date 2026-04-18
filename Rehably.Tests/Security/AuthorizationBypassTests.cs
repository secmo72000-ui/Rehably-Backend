using System.Text;
using FluentAssertions;
using Rehably.Tests.Helpers;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Rehably.Tests.Security;

/// <summary>
/// Tests to verify that authentication and authorization cannot be bypassed.
/// Ensures protected endpoints reject unauthorized requests correctly.
/// </summary>
public class AuthorizationBypassTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationBypassTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Unauthenticated Access

    [Fact]
    public async Task ProtectedEndpoint_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/packages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithNoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/clinics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Systematic Admin Endpoint 401 Checks

    [Theory]
    [InlineData("/api/admin/clinics")]
    [InlineData("/api/admin/packages")]
    [InlineData("/api/admin/features")]
    [InlineData("/api/admin/subscriptions")]
    [InlineData("/api/admin/invoices")]
    [InlineData("/api/admin/audit-logs")]
    [InlineData("/api/admin/platform-users")]
    [InlineData("/api/admin/roles")]
    [InlineData("/api/admin/permissions")]
    [InlineData("/api/admin/feature-categories")]
    public async Task AdminEndpoint_WithNoToken_Returns401(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"endpoint {endpoint} should reject unauthenticated requests");
    }

    [Theory]
    [InlineData("/api/admin/clinics")]
    [InlineData("/api/admin/packages")]
    [InlineData("/api/admin/features")]
    [InlineData("/api/admin/subscriptions")]
    [InlineData("/api/admin/invoices")]
    [InlineData("/api/admin/audit-logs")]
    [InlineData("/api/admin/platform-users")]
    [InlineData("/api/admin/roles")]
    [InlineData("/api/admin/permissions")]
    [InlineData("/api/admin/feature-categories")]
    public async Task AdminEndpoint_WithGarbageToken_Returns401(string endpoint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "garbage.not.a.jwt");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"endpoint {endpoint} should reject garbage tokens");
    }

    #endregion

    #region Invalid Token Tests

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_ReturnsUnauthorized()
    {
        const string tamperedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJoYWNrZXIiLCJyb2xlIjoiUGxhdGZvcm1BZG1pbiJ9.INVALID_SIGNATURE";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        var expiredToken = JwtTokenGenerator.ExpiredToken();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithAlgNoneAttack_ReturnsUnauthorized()
    {
        // Craft a JWT with alg:none — a classic JWT bypass attack
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"))
            .TrimEnd('=');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                "{\"sub\":\"hacker\",\"role\":\"PlatformAdmin\",\"Permission\":\"*.*\",\"exp\":9999999999}"))
            .TrimEnd('=');
        var algNoneToken = $"{header}.{payload}.";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", algNoneToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithAlgNoneNoSignature_ReturnsUnauthorized()
    {
        // Variant: alg none with empty signature segment
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"none\"}"))
            .TrimEnd('=');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                "{\"sub\":\"admin\",\"role\":\"PlatformAdmin\",\"Permission\":\"*.*\"}"))
            .TrimEnd('=');
        var token = $"{header}.{payload}.";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithWrongSigningKey_ReturnsUnauthorized()
    {
        // Generate a valid-format JWT signed with the wrong key
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("completely-wrong-signing-key-that-is-long-enough-for-hmac256!!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "hacker"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "PlatformAdmin"),
            new System.Security.Claims.Claim("Permission", "*.*")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "rehably",
            audience: "rehably-api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        var expiredToken = JwtTokenGenerator.ExpiredToken();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithEmptyBearerToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Security Headers

    [Fact]
    public async Task AllResponses_ContainXContentTypeOptionsHeader()
    {
        // Use /health endpoint which is always available and doesn't require auth
        var response = await _client.GetAsync("/health");

        response.Headers.TryGetValues("X-Content-Type-Options", out var values);
        values.Should().NotBeNull();
        values.Should().ContainSingle(v => v == "nosniff");
    }

    [Fact]
    public async Task AllResponses_ContainXFrameOptionsDenyHeader()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.TryGetValues("X-Frame-Options", out var values);
        values.Should().NotBeNull();
        values.Should().ContainSingle(v => v == "DENY");
    }

    [Theory]
    [InlineData("/api/admin/clinics")]
    [InlineData("/api/admin/packages")]
    [InlineData("/api/admin/features")]
    public async Task AdminEndpoint_ResponseContainsSecurityHeaders(string endpoint)
    {
        // Even 401 responses should include security headers
        var response = await _client.GetAsync(endpoint);

        response.Headers.TryGetValues("X-Content-Type-Options", out var xContentType);
        xContentType.Should().NotBeNull($"{endpoint} should return X-Content-Type-Options");
        xContentType.Should().ContainSingle(v => v == "nosniff");

        response.Headers.TryGetValues("X-Frame-Options", out var xFrame);
        xFrame.Should().NotBeNull($"{endpoint} should return X-Frame-Options");
        xFrame.Should().ContainSingle(v => v == "DENY");
    }

    #endregion

    #region Role-Based Authorization

    [Fact]
    public async Task PlatformAdminEndpoint_WithInvalidToken_RejectsRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "clinic.user.fake.token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithTenantUserToken_ReturnsForbidden()
    {
        var tenantToken = JwtTokenGenerator.TenantUserToken(Guid.NewGuid(), "patients.view");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var response = await _client.SendAsync(request);

        // Tenant users should not access admin endpoints
        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden]);
    }

    [Fact]
    public async Task AdminEndpoint_WithNoTenantToken_ButWildcardPermission_AllowsAccess()
    {
        var noTenantToken = JwtTokenGenerator.TokenWithNoTenant();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noTenantToken);

        var response = await _client.SendAsync(request);

        // TokenWithNoTenant has *.* permission which grants admin access even without tenant
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
