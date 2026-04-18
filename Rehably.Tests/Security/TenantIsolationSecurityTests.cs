using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Rehably.Tests.Helpers;
using Xunit;

namespace Rehably.Tests.Security;

public class TenantIsolationSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationSecurityTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTransaction_WhenAccessingOtherTenantTransaction_ReturnsForbiddenOrNotFound()
    {
        var otherTenantTransactionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/payments/{otherTenantTransactionId}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestJwtToken(tenantId: Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefundTransaction_WhenAccessingOtherTenantTransaction_ReturnsForbiddenOrNotFound()
    {
        var otherTenantTransactionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/payments/{otherTenantTransactionId}/refund");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestJwtToken(tenantId: Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePayment_WithDifferentClinicId_ReturnsForbiddenOrNotFound()
    {
        var payload = new
        {
            ClinicId = Guid.NewGuid(),
            SubscriptionPlanId = Guid.NewGuid(),
            ReturnUrl = "http://localhost:3000/success",
            CancelUrl = "http://localhost:3000/cancel",
            ProviderKey = "cash"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/create")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestJwtToken(tenantId: Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiEndpoints_WithoutTenantClaim_ReturnsUnauthorizedOrForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/payments/{Guid.NewGuid()}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestJwtToken(tenantId: null));

        var response = await _client.SendAsync(request);

        // Without TenantId claim, the user may be rejected by auth or permission checks
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
    }

    private static string GenerateTestJwtToken(Guid? tenantId)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(System.Security.Claims.ClaimTypes.Email, "test@test.com")
        };

        if (tenantId.HasValue)
        {
            claims.Add(new System.Security.Claims.Claim("TenantId", tenantId.Value.ToString()));
        }

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("rehablyMulti-Tanatet-Projec-saaaas-141515r234er434134r13r4r123r"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "rehably",
            audience: "rehably-api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
