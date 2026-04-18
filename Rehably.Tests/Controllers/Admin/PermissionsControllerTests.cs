using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.DTOs.Role;
using Rehably.Domain.Entities.Identity;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Controllers.Admin;

public class PermissionsControllerTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<ILogger<PermissionsController>> _loggerMock;
    private readonly PermissionsController _sut;

    public PermissionsControllerTests()
    {
        _roleManagerMock = MockHelpers.CreateMockRoleManager();
        _loggerMock = new Mock<ILogger<PermissionsController>>();
        _sut = new PermissionsController(_roleManagerMock.Object, _loggerMock.Object);
    }

    #region GetPermissions

    [Fact]
    public void GetPermissions_ReturnsOk()
    {
        var result = _sut.GetPermissions();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as Rehably.Application.Common.ApiResponse<PagedResult<PermissionDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPermissions_WithPagination_ReturnsOk()
    {
        var result = _sut.GetPermissions(page: 1, pageSize: 5);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as Rehably.Application.Common.ApiResponse<PagedResult<PermissionDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Items.Count.Should().BeLessThanOrEqualTo(5);
        response.Data.PageSize.Should().Be(5);
    }

    #endregion

    #region GetPlatformPermissions

    [Fact]
    public void GetPlatformPermissions_ReturnsOk()
    {
        var result = _sut.GetPlatformPermissions();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as Rehably.Application.Common.ApiResponse<PlatformPermissionMatrixResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Resources.Should().NotBeEmpty();
    }

    #endregion
}
