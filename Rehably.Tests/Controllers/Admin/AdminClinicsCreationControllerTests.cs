using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.Tests.Controllers.Admin;

public class AdminClinicsCreationControllerTests
{
    private readonly Mock<IClinicActivationService> _activationServiceMock;
    private readonly AdminClinicsCreationController _sut;

    public AdminClinicsCreationControllerTests()
    {
        _activationServiceMock = new Mock<IClinicActivationService>();
        _sut = new AdminClinicsCreationController(_activationServiceMock.Object);
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

    private static CreateClinicRequest CreateValidRequest() => new()
    {
        ClinicName = "Test Clinic",
        Phone = "+201234567890",
        Email = "clinic@test.com",
        PackageId = Guid.NewGuid(),
        OwnerEmail = "owner@test.com",
        OwnerFirstName = "John",
        OwnerLastName = "Doe",
        PaymentType = PaymentType.Cash,
        City = "Cairo",
        Country = "Egypt"
    };

    #region CreateClinic

    [Fact]
    public async Task CreateClinic_ValidRequest_Returns201WithClinicCreatedDto()
    {
        // Arrange
        var request = CreateValidRequest();
        var clinicId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var createdDto = new ClinicCreatedDto
        {
            Id = clinicId,
            Name = request.ClinicName,
            Email = request.OwnerEmail,
            Phone = request.Phone,
            Status = ClinicStatus.Active,
            SubscriptionId = subscriptionId,
            PackageName = "Pro",
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionStartDate = DateTime.UtcNow,
            PaymentType = "Cash",
            CreatedAt = DateTime.UtcNow
        };

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Success(createdDto));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ClinicCreatedDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(clinicId);
        response.Data.Name.Should().Be("Test Clinic");
        response.Data.SubscriptionId.Should().Be(subscriptionId);
    }

    [Fact]
    public async Task CreateClinic_ServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Failure("Failed to create clinic"));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<ClinicCreatedDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateClinic_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Failure("A user with this email already exists"));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<ClinicCreatedDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateClinic_InvalidPackage_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Failure("Package not found"));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<ClinicCreatedDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Error!.Message.Should().Contain("Package not found");
    }

    [Fact]
    public async Task CreateClinic_WithCashPayment_PassesRequestToService()
    {
        // Arrange
        var request = CreateValidRequest() with { PaymentType = PaymentType.Cash };
        var createdDto = new ClinicCreatedDto
        {
            Id = Guid.NewGuid(),
            Name = request.ClinicName,
            Email = request.OwnerEmail,
            PaymentType = "Cash",
            Status = ClinicStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(
                It.Is<CreateClinicRequest>(r => r.PaymentType == PaymentType.Cash),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Success(createdDto));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        _activationServiceMock.Verify(x => x.ActivateNewClinicAsync(
            It.Is<CreateClinicRequest>(r => r.PaymentType == PaymentType.Cash),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateClinic_WithFreePayment_PassesRequestToService()
    {
        // Arrange
        var request = CreateValidRequest() with { PaymentType = PaymentType.Free };
        var createdDto = new ClinicCreatedDto
        {
            Id = Guid.NewGuid(),
            Name = request.ClinicName,
            Email = request.OwnerEmail,
            PaymentType = "Free",
            Status = ClinicStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(
                It.Is<CreateClinicRequest>(r => r.PaymentType == PaymentType.Free),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Success(createdDto));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateClinic_ActivationFailure_ReturnsBadRequestWithBusinessRuleViolation()
    {
        // Arrange
        var request = CreateValidRequest();

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ClinicCreatedDto>.Failure("Failed to activate clinic"));

        // Act
        var result = await _sut.CreateClinic(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<ClinicCreatedDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(ErrorCodes.BusinessRuleViolation);
    }

    [Fact]
    public async Task CreateClinic_CancellationRequested_PassesCancellationToken()
    {
        // Arrange
        var request = CreateValidRequest();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _activationServiceMock
            .Setup(x => x.ActivateNewClinicAsync(request, token))
            .ReturnsAsync(Result<ClinicCreatedDto>.Success(new ClinicCreatedDto
            {
                Id = Guid.NewGuid(),
                Name = request.ClinicName,
                CreatedAt = DateTime.UtcNow
            }));

        // Act
        var result = await _sut.CreateClinic(request, token);

        // Assert
        _activationServiceMock.Verify(x => x.ActivateNewClinicAsync(request, token), Times.Once);
    }

    #endregion
}
