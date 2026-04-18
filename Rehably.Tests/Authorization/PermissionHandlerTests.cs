using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.API.Authorization;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Identity;
using System.Security.Claims;
using Xunit;

namespace Rehably.Tests.Authorization;

public class PermissionHandlerTests
{
    private readonly Mock<IPlanPermissionService> _planPermissionServiceMock;
    private readonly Mock<IPermissionLookupService> _permissionLookupServiceMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<PermissionHandler>> _loggerMock;
    private readonly PermissionHandler _handler;
    private readonly AuthorizationHandlerContext _context;
    private readonly DefaultHttpContext _httpContext;

    public PermissionHandlerTests()
    {
        _planPermissionServiceMock = new Mock<IPlanPermissionService>();
        _permissionLookupServiceMock = new Mock<IPermissionLookupService>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<PermissionHandler>>();
        _httpContext = new DefaultHttpContext();

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(_httpContext);

        _handler = new PermissionHandler(
            _planPermissionServiceMock.Object,
            _permissionLookupServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);

        var requirements = new[] { new PermissionRequirement("clinics.view") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim("TenantId", "123")
        }, "TestAuth"));

        _context = new AuthorizationHandlerContext(requirements, user, null);
    }

    [Fact]
    public async Task HandleRequirement_UserHasPermissionInClaims_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim("Permission", "clinics.view")
        }, "TestAuth"));
        var requirement = new PermissionRequirement("clinics.view");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_UserHasPermissionViaWildcard_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim("Permission", "clinics.*")
        }, "TestAuth"));
        var requirement = new PermissionRequirement("clinics.view");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
