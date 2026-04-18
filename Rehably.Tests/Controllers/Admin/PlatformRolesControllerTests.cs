using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Services.Admin;

namespace Rehably.Tests.Controllers.Admin;

public class PlatformRolesControllerTests
{
    private readonly Mock<IPlatformRoleService> _roleServiceMock;
    private readonly Mock<IPlatformRoleManagementService> _roleManagementServiceMock;
    private readonly PlatformRolesController _sut;
    private const string CurrentUserId = "admin-user-id";

    public PlatformRolesControllerTests()
    {
        _roleServiceMock = new Mock<IPlatformRoleService>();
        _roleManagementServiceMock = new Mock<IPlatformRoleManagementService>();
        _sut = new PlatformRolesController(_roleServiceMock.Object, _roleManagementServiceMock.Object);
        SetupAuthenticatedUser(CurrentUserId);
    }

    #region GetAll

    [Fact]
    public async Task GetAll_WhenRolesExist_ReturnsOkWithList()
    {
        var roles = new List<PlatformRoleResponse>
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Super Admin", UserCount = 1 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Support", UserCount = 3 }
        };
        _roleServiceMock
            .Setup(x => x.GetAllRolesAsync())
            .ReturnsAsync(Result<List<PlatformRoleResponse>>.Success(roles));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _roleServiceMock
            .Setup(x => x.GetAllRolesAsync())
            .ReturnsAsync(Result<List<PlatformRoleResponse>>.Success(new List<PlatformRoleResponse>()));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new CreatePlatformRoleRequest
        {
            Name = "Viewer",
            Description = "Read-only role",
            Permissions = new List<string> { "roles.view", "clinics.view" }
        };
        var created = new PlatformRoleResponse
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Viewer",
            Description = "Read-only role",
            Permissions = new List<string> { "roles.view", "clinics.view" }
        };
        _roleManagementServiceMock
            .Setup(x => x.CreateRoleAsync(request, CurrentUserId))
            .ReturnsAsync(Result<PlatformRoleResponse>.Success(created));

        var result = await _sut.Create(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var request = new CreatePlatformRoleRequest { Name = "Super Admin" };
        _roleManagementServiceMock
            .Setup(x => x.CreateRoleAsync(request, CurrentUserId))
            .ReturnsAsync(Result<PlatformRoleResponse>.Failure("Role with this name already exists"));

        var result = await _sut.Create(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(409);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        var roleId = Guid.NewGuid().ToString();
        var request = new UpdatePlatformRoleRequest
        {
            Description = "Updated description",
            Permissions = new List<string> { "roles.view" }
        };
        var updated = new PlatformRoleResponse
        {
            Id = roleId,
            Name = "Viewer",
            Description = "Updated description"
        };
        _roleManagementServiceMock
            .Setup(x => x.UpdateRoleAsync(roleId, request, CurrentUserId))
            .ReturnsAsync(Result<PlatformRoleResponse>.Success(updated));

        var result = await _sut.Update(roleId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var roleId = Guid.NewGuid().ToString();
        var request = new UpdatePlatformRoleRequest { Description = "Updated" };
        _roleManagementServiceMock
            .Setup(x => x.UpdateRoleAsync(roleId, request, CurrentUserId))
            .ReturnsAsync(Result<PlatformRoleResponse>.Failure("Role not found"));

        var result = await _sut.Update(roleId, request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_UnusedRole_ReturnsOk()
    {
        var roleId = Guid.NewGuid().ToString();
        _roleManagementServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Delete(roleId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_RoleWithUsers_Returns400()
    {
        var roleId = Guid.NewGuid().ToString();
        _roleManagementServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId))
            .ReturnsAsync(Result.Failure("Cannot delete role with assigned users"));

        var result = await _sut.Delete(roleId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var roleId = Guid.NewGuid().ToString();
        _roleManagementServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId))
            .ReturnsAsync(Result.Failure("Role not found"));

        var result = await _sut.Delete(roleId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region Helpers

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, "PlatformAdmin"),
            new("Permission", "*.*")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #endregion
}
