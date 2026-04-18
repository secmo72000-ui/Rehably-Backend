using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.Tests.Controllers.Admin;

public class AddOnsControllerTests
{
    private readonly Mock<IAddOnService> _addOnServiceMock;
    private readonly AddOnsController _sut;

    public AddOnsControllerTests()
    {
        _addOnServiceMock = new Mock<IAddOnService>();
        _sut = new AddOnsController(_addOnServiceMock.Object);
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

    #region GetAddOns

    [Fact]
    public async Task GetAddOns_ExistingClinic_ReturnsOkWithList()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var addOns = new List<AddOnDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FeatureId = Guid.NewGuid(),
                FeatureName = "Extra Patients",
                Limit = 50,
                Price = 100m,
                Status = AddOnStatus.Active,
                PaymentType = PaymentType.Cash,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                FeatureId = Guid.NewGuid(),
                FeatureName = "Extra Storage",
                Limit = 10,
                Price = 50m,
                Status = AddOnStatus.Active,
                PaymentType = PaymentType.Cash,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1)
            }
        };

        _addOnServiceMock
            .Setup(x => x.GetClinicAddOnsAsync(clinicId, null))
            .ReturnsAsync(Result<List<AddOnDto>>.Success(addOns));

        // Act
        var result = await _sut.GetAddOns(clinicId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<AddOnDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAddOns_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var addOns = new List<AddOnDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FeatureName = "Extra Patients",
                Status = AddOnStatus.Active
            }
        };

        _addOnServiceMock
            .Setup(x => x.GetClinicAddOnsAsync(clinicId, AddOnStatus.Active))
            .ReturnsAsync(Result<List<AddOnDto>>.Success(addOns));

        // Act
        var result = await _sut.GetAddOns(clinicId, AddOnStatus.Active);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _addOnServiceMock.Verify(x => x.GetClinicAddOnsAsync(clinicId, AddOnStatus.Active), Times.Once);
    }

    [Fact]
    public async Task GetAddOns_NoAddOns_ReturnsOkWithEmptyList()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _addOnServiceMock
            .Setup(x => x.GetClinicAddOnsAsync(clinicId, null))
            .ReturnsAsync(Result<List<AddOnDto>>.Success([]));

        // Act
        var result = await _sut.GetAddOns(clinicId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<AddOnDto>>>().Subject;
        response.Data!.Should().BeEmpty();
    }

    #endregion

    #region GetAvailableAddOns

    [Fact]
    public async Task GetAvailableAddOns_ExistingClinic_ReturnsOkWithAvailableList()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var available = new List<AvailableAddOnDto>
        {
            new()
            {
                FeatureId = Guid.NewGuid(),
                FeatureName = "Extra Patients",
                FeatureCode = "patients",
                BasePrice = 100m,
                PerUnitPrice = 2m,
                MinQuantity = 1,
                MaxQuantity = 100,
                CurrentAddonLimit = 0
            },
            new()
            {
                FeatureId = Guid.NewGuid(),
                FeatureName = "Extra Storage",
                FeatureCode = "storage",
                BasePrice = 50m,
                PerUnitPrice = 5m,
                MinQuantity = 1,
                MaxQuantity = 50,
                CurrentAddonLimit = 10
            }
        };

        _addOnServiceMock
            .Setup(x => x.GetAvailableAddOnsAsync(clinicId))
            .ReturnsAsync(Result<List<AvailableAddOnDto>>.Success(available));

        // Act
        var result = await _sut.GetAvailableAddOns(clinicId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<AvailableAddOnDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Should().HaveCount(2);
        response.Data.Should().Contain(a => a.FeatureCode == "patients" && a.CurrentAddonLimit == 0);
        response.Data.Should().Contain(a => a.FeatureCode == "storage" && a.CurrentAddonLimit == 10);
    }

    [Fact]
    public async Task GetAvailableAddOns_ServiceFailure_ReturnsError()
    {
        // Arrange
        var clinicId = Guid.NewGuid();

        _addOnServiceMock
            .Setup(x => x.GetAvailableAddOnsAsync(clinicId))
            .ReturnsAsync(Result<List<AvailableAddOnDto>>.Failure("Clinic not found"));

        // Act
        var result = await _sut.GetAvailableAddOns(clinicId);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region CreateAddOn

    [Fact]
    public async Task CreateAddOn_ValidRequest_Returns201WithAddOn()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var request = new CreateAddOnRequestDto
        {
            FeatureId = featureId,
            Limit = 50,
            Price = 100m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            PaymentType = PaymentType.Cash
        };

        var createdAddOn = new AddOnDto
        {
            Id = Guid.NewGuid(),
            FeatureId = featureId,
            FeatureName = "Extra Patients",
            Limit = 50,
            Price = 100m,
            Status = AddOnStatus.Active,
            PaymentType = PaymentType.Cash,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _addOnServiceMock
            .Setup(x => x.CreateAddOnAsync(clinicId, request))
            .ReturnsAsync(Result<AddOnDto>.Success(createdAddOn));

        // Act
        var result = await _sut.CreateAddOn(clinicId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<AddOnDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.FeatureId.Should().Be(featureId);
        response.Data.Limit.Should().Be(50);
        response.Data.Status.Should().Be(AddOnStatus.Active);
    }

    [Fact]
    public async Task CreateAddOn_InvalidFeature_ReturnsError()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new CreateAddOnRequestDto
        {
            FeatureId = Guid.NewGuid(),
            Limit = 10,
            Price = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            PaymentType = PaymentType.Cash
        };

        _addOnServiceMock
            .Setup(x => x.CreateAddOnAsync(clinicId, request))
            .ReturnsAsync(Result<AddOnDto>.Failure("Feature not found"));

        // Act
        var result = await _sut.CreateAddOn(clinicId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAddOn_NoActiveSubscription_ReturnsError()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new CreateAddOnRequestDto
        {
            FeatureId = Guid.NewGuid(),
            Limit = 10,
            Price = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            PaymentType = PaymentType.Cash
        };

        _addOnServiceMock
            .Setup(x => x.CreateAddOnAsync(clinicId, request))
            .ReturnsAsync(Result<AddOnDto>.Failure("Cannot create add-on: no active subscription"));

        // Act
        var result = await _sut.CreateAddOn(clinicId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region CancelAddOn

    [Fact]
    public async Task CancelAddOn_ActiveAddOn_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var addOnId = Guid.NewGuid();

        _addOnServiceMock
            .Setup(x => x.CancelAddOnAsync(clinicId, addOnId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.CancelAddOn(clinicId, addOnId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CancelAddOn_AlreadyCancelled_ReturnsError()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var addOnId = Guid.NewGuid();

        _addOnServiceMock
            .Setup(x => x.CancelAddOnAsync(clinicId, addOnId))
            .ReturnsAsync(Result.Failure("Cannot cancel: add-on is already cancelled"));

        // Act
        var result = await _sut.CancelAddOn(clinicId, addOnId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CancelAddOn_NonExistingAddOn_Returns404()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var addOnId = Guid.NewGuid();

        _addOnServiceMock
            .Setup(x => x.CancelAddOnAsync(clinicId, addOnId))
            .ReturnsAsync(Result.Failure("Add-on not found"));

        // Act
        var result = await _sut.CancelAddOn(clinicId, addOnId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion
}
