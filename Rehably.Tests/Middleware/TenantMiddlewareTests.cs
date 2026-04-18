using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rehably.API.Middleware;
using Rehably.Application.Contexts;
using Rehably.Application.Services.Clinic;
using System.Security.Claims;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Rehably.Domain.Entities.Tenant;
using Rehably.Infrastructure.Data;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Middleware;

public class TenantMiddlewareTests
{
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ITenantResolutionService> _tenantResolutionServiceMock;
    private readonly TenantMiddleware _middleware;
    private readonly Guid _testClinicId = Guid.NewGuid();

    public TenantMiddlewareTests()
    {
        _tenantContextMock = new Mock<ITenantContext>();
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();

        // Create real in-memory DB context with mock tenant context for query filters
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns((Guid?)null);
        _dbContext = new ApplicationDbContext(options, null, mockTenantContext.Object);

        _tenantResolutionServiceMock = new Mock<ITenantResolutionService>();
        _tenantResolutionServiceMock
            .Setup(s => s.ResolveClinicAsync(_testClinicId))
            .ReturnsAsync((ClinicStatus.Active, true));

        _middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object);

        // Seed clinic
        _dbContext.Clinics.Add(new Clinic { Id = _testClinicId, Name = "Test Clinic", Status = ClinicStatus.Active, Slug = "test-clinic", Phone = "123" });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Invoke_ValidTenant_SetsTenantContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("TenantId", _testClinicId.ToString())
        }, "TestAuth"));
        context.User = user;

        // Act
        await _middleware.InvokeAsync(context, _tenantContextMock.Object, _dbContext, _tenantResolutionServiceMock.Object);

        // Assert
        _tenantContextMock.Verify(t => t.SetTenant(_testClinicId), Times.Once);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_InvalidTenant_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("TenantId", "invalid")
        }, "TestAuth"));
        context.User = user;

        // Act
        await _middleware.InvokeAsync(context, _tenantContextMock.Object, _dbContext, _tenantResolutionServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task Invoke_AnonymousPath_SkipsTenantCheck()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";

        // Act
        await _middleware.InvokeAsync(context, _tenantContextMock.Object, _dbContext, _tenantResolutionServiceMock.Object);

        // Assert
        _tenantContextMock.Verify(t => t.SetTenant(It.IsAny<Guid>()), Times.Never);
        _nextMock.Verify(n => n(context), Times.Once);
    }
}
