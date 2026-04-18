using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Rehably.API.Middleware;
using Rehably.Tests.Helpers;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Rehably.Tests.Middleware;

public class MustChangePasswordMiddlewareTests
{
    private readonly MustChangePasswordMiddleware _middleware;
    private readonly Mock<RequestDelegate> _nextMock;

    public MustChangePasswordMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new MustChangePasswordMiddleware(_nextMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_UserNotAuthenticated_CallsNextDelegate()
    {
        var context = CreateHttpContext(user: null);

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_UserAuthenticatedWithoutMustChangePasswordClaim_CallsNextDelegate()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: false);
        var context = CreateHttpContext(user);

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_UserWithMustChangePasswordFalse_CallsNextDelegate()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: false);
        var context = CreateHttpContext(user);

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_ToChangePasswordEndpoint_CallsNextDelegate()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, "/api/auth/change-password");

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_ToOtherEndpoint_Returns403()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, "/api/patients");

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Never);
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_ReturnsCorrectErrorResponse()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, "/api/patients");
        context.Response.Body = new MemoryStream();

        await _middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseJson = await reader.ReadToEndAsync();

        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);

        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("error").GetString().Should().Be("You must change your password before accessing this resource");
    }

    [Fact]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_ToRootEndpoint_Returns403()
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, "/");

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Never);
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Theory]
    [InlineData("/api/auth/change-password")]
    [InlineData("/api/auth/change-password/")]
    [InlineData("/API/AUTH/CHANGE-PASSWORD")]
    [InlineData("/Api/Auth/Change-Password")]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_AllowsChangePasswordEndpointVariations(string path)
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, path);

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/patients")]
    [InlineData("/api/appointments")]
    [InlineData("/api/users")]
    public async Task InvokeAsync_UserWithMustChangePasswordTrue_BlocksOtherEndpoints(string path)
    {
        var user = JwtTestHelper.CreateClaimsPrincipal("user-123", tenantId: 1, mustChangePassword: true);
        var context = CreateHttpContext(user, path);

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(x => x(context), Times.Never);
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user, string path = "/api/test")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.User = user ?? new ClaimsPrincipal();

        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }
}
