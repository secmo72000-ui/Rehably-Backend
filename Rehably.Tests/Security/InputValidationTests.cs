using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Rehably.Tests.Helpers;
using Xunit;

namespace Rehably.Tests.Security;

public class InputValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InputValidationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region Existing SQL Injection & XSS Tests

    [Fact]
    public async Task SearchEndpoint_WithSqlInjection_ReturnsBadRequestOrSanitized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/admin/clinics?search={Uri.EscapeDataString("'; DROP TABLE Clinics; --")}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SearchEndpoint_WithXssPayload_ReturnsBadRequestOrSanitized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/admin/clinics?search={Uri.EscapeDataString("<script>alert('xss')</script>")}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Auth Validation Tests

    [Fact]
    public async Task Auth_WithEmptyEmail_ReturnsBadRequest()
    {
        var payload = new
        {
            email = "",
            password = "Test123!@#"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Auth_WithInvalidEmail_ReturnsBadRequest()
    {
        var payload = new
        {
            email = "invalid-email",
            password = "Test123!@#"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Auth_WithExcessiveLengthFields_ReturnsBadRequest()
    {
        var payload = new
        {
            email = new string('A', 10000) + "@test.com",
            password = "Test123!@#"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Advanced SQL Injection Tests

    [Fact]
    public async Task SearchEndpoint_WithUnionSelectInjection_DoesNotReturn500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/admin/clinics?search={Uri.EscapeDataString("' UNION SELECT * FROM users --")}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "SQL injection via UNION SELECT should be handled safely");
    }

    #endregion

    #region XSS & HTML Injection Tests

    [Fact]
    public async Task AdminClinics_WithXssInClinicName_DoesNotReturn500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clinics");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());
        request.Content = new StringContent(
            """
            {
                "name": "<script>alert(1)</script>",
                "email": "xss@test.com",
                "phone": "+1234567890",
                "ownerFirstName": "Test",
                "ownerLastName": "User"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "XSS payloads in clinic name should be handled safely");

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("<script>",
                "response should not reflect raw script tags");
        }
    }

    #endregion

    #region Path Traversal Tests

    [Fact]
    public async Task SearchEndpoint_WithPathTraversal_DoesNotReturn500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/admin/clinics?search={Uri.EscapeDataString("../../etc/passwd")}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "path traversal payloads should be handled safely");

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("root:",
                "response should not contain file system contents");
        }
    }

    #endregion

    #region Request Body Validation Tests

    [Fact]
    public async Task AdminEndpoint_WithOversizedRequestBody_RejectsGracefully()
    {
        // Build a JSON body over 10MB
        var largeString = new string('X', 11 * 1024 * 1024);
        var json = $"{{\"name\":\"{largeString}\"}}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clinics");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "oversized request bodies should be rejected, not cause server errors");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.UnsupportedMediaType,
            (HttpStatusCode)413);
    }

    [Fact]
    public async Task AdminEndpoint_WithInvalidJson_ReturnsBadRequestOrUnsupportedMedia()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clinics");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());
        request.Content = new StringContent(
            "{invalid json: not valid, missing quotes",
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "malformed JSON should not cause a server error");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task AdminEndpoint_WithMissingContentType_DoesNotReturn500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clinics");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());
        request.Content = new StringContent(
            "{\"name\":\"Test Clinic\"}",
            Encoding.UTF8);
        // Remove Content-Type header to simulate missing media type
        request.Content.Headers.ContentType = null;

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "missing Content-Type header should be handled gracefully");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Null Byte Injection Tests

    [Fact]
    public async Task SearchEndpoint_WithNullByteInjection_DoesNotReturn500()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/admin/clinics?search={Uri.EscapeDataString("clinic\0admin")}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenGenerator.PlatformAdminToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "null byte injection should not cause server errors");
    }

    [Fact]
    public async Task Auth_WithNullByteInEmail_DoesNotReturn500()
    {
        var payload = new
        {
            email = "test\0@test.com",
            password = "Test123!@#"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "null byte in email should be handled safely");
    }

    #endregion

    #region IDOR (Insecure Direct Object Reference) Tests

    [Fact]
    public async Task AdminClinic_WithRandomGuidWithoutAuth_ReturnsUnauthorized()
    {
        var randomClinicId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/admin/clinics/{randomClinicId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "accessing a specific clinic resource without auth should be rejected");
    }

    [Fact]
    public async Task AdminEndpoint_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "accessing an admin resource without auth should be rejected");
    }

    #endregion
}
