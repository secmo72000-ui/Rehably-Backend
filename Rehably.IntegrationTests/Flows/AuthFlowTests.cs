using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Rehably.IntegrationTests.Infrastructure;

namespace Rehably.IntegrationTests.Flows;

[Collection("IntegrationTests")]
[TestCaseOrderer("Rehably.IntegrationTests.Infrastructure.PriorityOrderer", "Rehably.IntegrationTests")]
[Trait("Category", "Integration")]
public class AuthFlowTests : FlowTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string? _accessToken;
    private static string? _refreshToken;
    private static readonly string TestEmail = "user@test.com";
    private static readonly string TestPassword = "TempPassword123!";
    private static readonly string NewPassword = "NewSecureP@ss456!";

    public AuthFlowTests(RehablyWebApplicationFactory factory) : base(factory) { }

    [Fact, TestPriority(1)]
    public async Task Login_ValidCredentials_ReturnsJwtAndRefreshToken()
    {
        var loginResult = await LoginFullAsync(TestEmail, TestPassword);

        loginResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        loginResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _accessToken = loginResult.AccessToken;
        _refreshToken = loginResult.RefreshToken;
    }

    [Fact, TestPriority(2)]
    public async Task GetMe_WithValidToken_Returns200WithEmail()
    {
        _accessToken.Should().NotBeNull("previous test must set the token");

        var response = await GetRawAsync("/api/auth/me", _accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(TestEmail);
    }

    [Fact, TestPriority(3)]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await GetRawAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact, TestPriority(4)]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewTokens()
    {
        _refreshToken.Should().NotBeNull("login test must set the refresh token");

        var response = await PostRawAsync("/api/auth/refresh",
            new { refreshToken = _refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, JsonOptions);

        result!.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        _accessToken = result.Data.AccessToken;
        _refreshToken = result.Data.RefreshToken;
    }

    [Fact, TestPriority(5)]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsSuccess()
    {
        _accessToken.Should().NotBeNull("refresh test must set the token");

        var response = await PostRawAsync("/api/auth/change-password",
            new { currentPassword = TestPassword, newPassword = NewPassword },
            _accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact, TestPriority(6)]
    public async Task Login_WithNewPassword_ReturnsSuccess()
    {
        var loginResult = await LoginFullAsync(TestEmail, NewPassword);

        loginResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        _accessToken = loginResult.AccessToken;
        _refreshToken = loginResult.RefreshToken;
    }

    [Fact, TestPriority(7)]
    public async Task Logout_WithValidToken_SucceedsThenRefreshFails()
    {
        _accessToken.Should().NotBeNull("login with new password must set the token");

        var logoutResponse = await PostRawAsync("/api/auth/logout", new { }, _accessToken);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // After logout, refresh with the old token should fail
        var refreshResponse = await PostRawAsync("/api/auth/refresh",
            new { refreshToken = _refreshToken });

        refreshResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact, TestPriority(8)]
    public async Task GetMe_WithInvalidToken_Returns401()
    {
        var response = await GetRawAsync("/api/auth/me", "invalid.jwt.token");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
