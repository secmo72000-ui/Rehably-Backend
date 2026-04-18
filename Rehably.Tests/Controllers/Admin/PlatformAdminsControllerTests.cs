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

public class PlatformAdminsControllerTests
{
    private readonly Mock<IPlatformAdminService> _adminServiceMock;
    private readonly Mock<IPlatformAdminManagementService> _adminManagementServiceMock;
    private readonly PlatformAdminsController _sut;
    private const string CurrentUserId = "current-admin-id";

    public PlatformAdminsControllerTests()
    {
        _adminServiceMock = new Mock<IPlatformAdminService>();
        _adminManagementServiceMock = new Mock<IPlatformAdminManagementService>();
        _sut = new PlatformAdminsController(_adminServiceMock.Object, _adminManagementServiceMock.Object);
        SetupAuthenticatedUser(CurrentUserId);
    }

    #region GetAll

    [Fact]
    public async Task GetAll_DefaultPagination_ReturnsOkWithPagedResult()
    {
        var pagedResult = new PagedResult<PlatformAdminResponse>(
            new List<PlatformAdminResponse>
            {
                new() { Id = "1", Email = "admin1@test.com", FirstName = "Admin", LastName = "One" }
            },
            totalCount: 1, page: 1, pageSize: 20);
        _adminServiceMock
            .Setup(x => x.GetAllAdminsAsync(1, 20))
            .ReturnsAsync(Result<PagedResult<PlatformAdminResponse>>.Success(pagedResult));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithCustomPagination_ReturnsCorrectPage()
    {
        var pagedResult = new PagedResult<PlatformAdminResponse>(
            new List<PlatformAdminResponse>(), totalCount: 5, page: 2, pageSize: 2);
        _adminServiceMock
            .Setup(x => x.GetAllAdminsAsync(2, 2))
            .ReturnsAsync(Result<PagedResult<PlatformAdminResponse>>.Success(pagedResult));

        var result = await _sut.GetAll(page: 2, pageSize: 2);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new CreatePlatformAdminRequest
        {
            Email = "new@test.com",
            FirstName = "New",
            LastName = "Admin",
            RoleId = Guid.NewGuid().ToString()
        };
        var created = new PlatformAdminResponse
        {
            Id = Guid.NewGuid().ToString(),
            Email = "new@test.com",
            FirstName = "New",
            LastName = "Admin",
            TemporaryPassword = "TempPass123!"
        };
        _adminManagementServiceMock
            .Setup(x => x.CreateAdminAsync(request))
            .ReturnsAsync(Result<PlatformAdminResponse>.Success(created));

        var result = await _sut.Create(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var request = new CreatePlatformAdminRequest
        {
            Email = "existing@test.com",
            FirstName = "Dup",
            LastName = "Admin",
            RoleId = Guid.NewGuid().ToString()
        };
        _adminManagementServiceMock
            .Setup(x => x.CreateAdminAsync(request))
            .ReturnsAsync(Result<PlatformAdminResponse>.Failure("User with this email already exists"));

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
        var adminId = Guid.NewGuid().ToString();
        var request = new UpdatePlatformAdminRequest { FirstName = "Updated", IsActive = true };
        var updated = new PlatformAdminResponse
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Updated",
            LastName = "Admin"
        };
        _adminManagementServiceMock
            .Setup(x => x.UpdateAdminAsync(adminId, request))
            .ReturnsAsync(Result<PlatformAdminResponse>.Success(updated));

        var result = await _sut.Update(adminId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        var adminId = Guid.NewGuid().ToString();
        var request = new UpdatePlatformAdminRequest { FirstName = "Updated" };
        _adminManagementServiceMock
            .Setup(x => x.UpdateAdminAsync(adminId, request))
            .ReturnsAsync(Result<PlatformAdminResponse>.Failure("Administrator not found"));

        var result = await _sut.Update(adminId, request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region ChangeRole

    [Fact]
    public async Task ChangeRole_ValidRequest_ReturnsOk()
    {
        var adminId = Guid.NewGuid().ToString();
        var request = new ChangeAdminRoleRequest { RoleId = Guid.NewGuid().ToString() };
        _adminManagementServiceMock
            .Setup(x => x.ChangeAdminRoleAsync(adminId, request, CurrentUserId))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ChangeRole(adminId, request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangeRole_SelfChange_Returns400()
    {
        var request = new ChangeAdminRoleRequest { RoleId = Guid.NewGuid().ToString() };
        _adminManagementServiceMock
            .Setup(x => x.ChangeAdminRoleAsync(CurrentUserId, request, CurrentUserId))
            .ReturnsAsync(Result.Failure("Cannot change your own role"));

        var result = await _sut.ChangeRole(CurrentUserId, request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_OtherAdmin_ReturnsOk()
    {
        var adminId = Guid.NewGuid().ToString();
        _adminManagementServiceMock
            .Setup(x => x.DeleteAdminAsync(adminId, CurrentUserId))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Delete(adminId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_SelfDelete_Returns400()
    {
        _adminManagementServiceMock
            .Setup(x => x.DeleteAdminAsync(CurrentUserId, CurrentUserId))
            .ReturnsAsync(Result.Failure("Cannot delete your own account"));

        var result = await _sut.Delete(CurrentUserId);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        var adminId = Guid.NewGuid().ToString();
        _adminManagementServiceMock
            .Setup(x => x.DeleteAdminAsync(adminId, CurrentUserId))
            .ReturnsAsync(Result.Failure("Administrator not found"));

        var result = await _sut.Delete(adminId);

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
