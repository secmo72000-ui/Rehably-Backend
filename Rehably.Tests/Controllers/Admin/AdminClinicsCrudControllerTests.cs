using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.Tests.Controllers.Admin;

public class AdminClinicsCrudControllerTests
{
    private readonly Mock<IClinicService> _clinicServiceMock;
    private readonly AdminClinicsCrudController _sut;

    public AdminClinicsCrudControllerTests()
    {
        _clinicServiceMock = new Mock<IClinicService>();
        _sut = new AdminClinicsCrudController(_clinicServiceMock.Object);
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.Role, "PlatformAdmin"),
            new Claim("Permission", "*.*")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GetClinics

    [Fact]
    public async Task GetClinics_ValidQuery_ReturnsOkWithPagedResult()
    {
        // Arrange
        var query = new GetClinicsQuery { Page = 1, PageSize = 10 };
        var clinics = new List<ClinicResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Clinic 1", Phone = "123" },
            new() { Id = Guid.NewGuid(), Name = "Test Clinic 2", Phone = "456" }
        };
        var pagedResult = new PagedResult<ClinicResponse>(clinics, 2, 1, 10);

        _clinicServiceMock
            .Setup(x => x.SearchClinicsAsync(It.IsAny<GetClinicsQuery>()))
            .ReturnsAsync(Result<PagedResult<ClinicResponse>>.Success(pagedResult));

        // Act
        var result = await _sut.GetClinics(query);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ClinicResponse>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(2);
        response.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetClinics_WithSearchTerm_PassesQueryToService()
    {
        // Arrange
        var query = new GetClinicsQuery { Page = 1, PageSize = 20, Search = "Cairo" };
        var pagedResult = new PagedResult<ClinicResponse>([], 0, 1, 20);

        _clinicServiceMock
            .Setup(x => x.SearchClinicsAsync(It.Is<GetClinicsQuery>(q => q.Search == "Cairo")))
            .ReturnsAsync(Result<PagedResult<ClinicResponse>>.Success(pagedResult));

        // Act
        var result = await _sut.GetClinics(query);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _clinicServiceMock.Verify(x => x.SearchClinicsAsync(
            It.Is<GetClinicsQuery>(q => q.Search == "Cairo")), Times.Once);
    }

    [Fact]
    public async Task GetClinics_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var query = new GetClinicsQuery { Status = ClinicStatus.Active };
        var pagedResult = new PagedResult<ClinicResponse>([], 0, 1, 20);

        _clinicServiceMock
            .Setup(x => x.SearchClinicsAsync(It.IsAny<GetClinicsQuery>()))
            .ReturnsAsync(Result<PagedResult<ClinicResponse>>.Success(pagedResult));

        // Act
        var result = await _sut.GetClinics(query);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _clinicServiceMock.Verify(x => x.SearchClinicsAsync(
            It.Is<GetClinicsQuery>(q => q.Status == ClinicStatus.Active)), Times.Once);
    }

    [Fact]
    public async Task GetClinics_ServiceFailure_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetClinicsQuery();

        _clinicServiceMock
            .Setup(x => x.SearchClinicsAsync(It.IsAny<GetClinicsQuery>()))
            .ReturnsAsync(Result<PagedResult<ClinicResponse>>.Failure("Something went wrong"));

        // Act
        var result = await _sut.GetClinics(query);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetClinic

    [Fact]
    public async Task GetClinic_ExistingId_ReturnsOkWithClinic()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var clinic = new ClinicResponse { Id = clinicId, Name = "Test Clinic", Phone = "123" };

        _clinicServiceMock
            .Setup(x => x.GetClinicByIdAsync(clinicId))
            .ReturnsAsync(Result<ClinicResponse>.Success(clinic));

        // Act
        var result = await _sut.GetClinic(clinicId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ClinicResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(clinicId);
        response.Data.Name.Should().Be("Test Clinic");
    }

    [Fact]
    public async Task GetClinic_NonExistingId_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.GetClinicByIdAsync(clinicId))
            .ReturnsAsync(Result<ClinicResponse>.Failure("Clinic not found"));

        // Act
        var result = await _sut.GetClinic(clinicId);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region UpdateClinic

    [Fact]
    public async Task UpdateClinic_ValidRequest_ReturnsOkWithUpdatedClinic()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UpdateClinicRequest { Name = "Updated Clinic", Phone = "999" };
        var updated = new ClinicResponse { Id = clinicId, Name = "Updated Clinic", Phone = "999" };

        _clinicServiceMock
            .Setup(x => x.UpdateClinicAsync(clinicId, request))
            .ReturnsAsync(Result<ClinicResponse>.Success(updated));

        // Act
        var result = await _sut.UpdateClinic(clinicId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ClinicResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Name.Should().Be("Updated Clinic");
    }

    [Fact]
    public async Task UpdateClinic_NonExistingId_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UpdateClinicRequest { Name = "Updated" };

        _clinicServiceMock
            .Setup(x => x.UpdateClinicAsync(clinicId, request))
            .ReturnsAsync(Result<ClinicResponse>.Failure("Clinic not found"));

        // Act
        var result = await _sut.UpdateClinic(clinicId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateClinic_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UpdateClinicRequest { Email = "bad-email" };

        _clinicServiceMock
            .Setup(x => x.UpdateClinicAsync(clinicId, request))
            .ReturnsAsync(Result<ClinicResponse>.Failure("Validation failed: invalid email"));

        // Act
        var result = await _sut.UpdateClinic(clinicId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region DeleteClinic

    [Fact]
    public async Task DeleteClinic_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.DeleteClinicAsync(clinicId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.DeleteClinic(clinicId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteClinic_NonExistingId_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.DeleteClinicAsync(clinicId))
            .ReturnsAsync(Result.Failure("Clinic not found"));

        // Act
        var result = await _sut.DeleteClinic(clinicId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region SuspendClinic

    [Fact]
    public async Task SuspendClinic_ActiveClinic_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.SuspendClinicAsync(clinicId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.SuspendClinic(clinicId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SuspendClinic_NonExistingClinic_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.SuspendClinicAsync(clinicId))
            .ReturnsAsync(Result.Failure("Clinic not found"));

        // Act
        var result = await _sut.SuspendClinic(clinicId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region ActivateClinic

    [Fact]
    public async Task ActivateClinic_SuspendedClinic_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.ActivateClinicAsync(clinicId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ActivateClinic(clinicId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ActivateClinic_NonExistingClinic_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.ActivateClinicAsync(clinicId))
            .ReturnsAsync(Result.Failure("Clinic not found"));

        // Act
        var result = await _sut.ActivateClinic(clinicId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region BanClinic

    [Fact]
    public async Task BanClinic_ActiveClinic_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new BanClinicRequest { Reason = "Terms of service violation" };

        _clinicServiceMock
            .Setup(x => x.BanClinicAsync(clinicId, request.Reason, It.IsAny<string>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.BanClinic(clinicId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BanClinic_NonExistingClinic_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new BanClinicRequest { Reason = "Violation" };

        _clinicServiceMock
            .Setup(x => x.BanClinicAsync(clinicId, request.Reason, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure("Clinic not found"));

        // Act
        var result = await _sut.BanClinic(clinicId, request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region UnbanClinic

    [Fact]
    public async Task UnbanClinic_BannedClinic_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UnbanClinicRequest { Reason = "Appeal accepted" };

        _clinicServiceMock
            .Setup(x => x.UnbanClinicAsync(clinicId, request.Reason, It.IsAny<string>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.UnbanClinic(clinicId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UnbanClinic_NonExistingClinic_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        UnbanClinicRequest? request = null;

        _clinicServiceMock
            .Setup(x => x.UnbanClinicAsync(clinicId, null, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure("Clinic not found"));

        // Act
        var result = await _sut.UnbanClinic(clinicId, request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UnbanClinic_NullRequest_PassesNullReasonToService()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _clinicServiceMock
            .Setup(x => x.UnbanClinicAsync(clinicId, null, It.IsAny<string>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.UnbanClinic(clinicId, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _clinicServiceMock.Verify(x => x.UnbanClinicAsync(clinicId, null, It.IsAny<string>()), Times.Once);
    }

    #endregion
}
