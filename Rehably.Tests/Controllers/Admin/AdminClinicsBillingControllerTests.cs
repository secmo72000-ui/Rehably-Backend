using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Payment;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Payment;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.Tests.Controllers.Admin;

public class AdminClinicsBillingControllerTests
{
    private readonly Mock<IClinicService> _clinicServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IPlatformSubscriptionService> _subscriptionServiceMock;
    private readonly Mock<ISubscriptionLifecycleService> _lifecycleServiceMock;
    private readonly Mock<ISubscriptionModificationService> _modificationServiceMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock;
    private readonly AdminClinicsBillingController _sut;

    public AdminClinicsBillingControllerTests()
    {
        _clinicServiceMock = new Mock<IClinicService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _subscriptionServiceMock = new Mock<IPlatformSubscriptionService>();
        _lifecycleServiceMock = new Mock<ISubscriptionLifecycleService>();
        _modificationServiceMock = new Mock<ISubscriptionModificationService>();
        _authServiceMock = new Mock<IAuthService>();
        _authPasswordServiceMock = new Mock<IAuthPasswordService>();

        _sut = new AdminClinicsBillingController(
            _clinicServiceMock.Object,
            _paymentServiceMock.Object,
            _subscriptionServiceMock.Object,
            _lifecycleServiceMock.Object,
            _modificationServiceMock.Object,
            _authServiceMock.Object,
            _authPasswordServiceMock.Object);

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

    #region UpdateSubscription

    [Fact]
    public async Task UpdateSubscription_ValidRequest_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var newPackageId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest { NewPackageId = newPackageId };

        var subscriptions = new List<SubscriptionDto>
        {
            new() { Id = subscriptionId, ClinicId = clinicId, Status = SubscriptionStatus.Active }
        };

        var upgradedDetail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            ClinicId = clinicId,
            PackageId = newPackageId,
            PackageName = "Pro",
            Status = SubscriptionStatus.Active
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsAsync(clinicId))
            .ReturnsAsync(Result<List<SubscriptionDto>>.Success(subscriptions));

        _modificationServiceMock
            .Setup(x => x.UpgradeSubscriptionAsync(subscriptionId, It.IsAny<UpgradeSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(upgradedDetail));

        // Act
        var result = await _sut.UpdateSubscription(clinicId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateSubscription_NoSubscriptionsFound_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest { NewPackageId = Guid.NewGuid() };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsAsync(clinicId))
            .ReturnsAsync(Result<List<SubscriptionDto>>.Failure("No subscriptions found"));

        // Act
        var result = await _sut.UpdateSubscription(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateSubscription_NoActiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest { NewPackageId = Guid.NewGuid() };

        var subscriptions = new List<SubscriptionDto>
        {
            new() { Id = Guid.NewGuid(), ClinicId = clinicId, Status = SubscriptionStatus.Cancelled }
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsAsync(clinicId))
            .ReturnsAsync(Result<List<SubscriptionDto>>.Success(subscriptions));

        // Act
        var result = await _sut.UpdateSubscription(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateSubscription_UpgradeFails_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest { NewPackageId = Guid.NewGuid() };

        var subscriptions = new List<SubscriptionDto>
        {
            new() { Id = subscriptionId, ClinicId = clinicId, Status = SubscriptionStatus.Active }
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsAsync(clinicId))
            .ReturnsAsync(Result<List<SubscriptionDto>>.Success(subscriptions));

        _modificationServiceMock
            .Setup(x => x.UpgradeSubscriptionAsync(subscriptionId, It.IsAny<UpgradeSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Cannot downgrade subscription"));

        // Act
        var result = await _sut.UpdateSubscription(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateSubscription_TrialSubscription_CanBeUpgraded()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var newPackageId = Guid.NewGuid();
        var request = new UpdateSubscriptionRequest { NewPackageId = newPackageId };

        var subscriptions = new List<SubscriptionDto>
        {
            new() { Id = subscriptionId, ClinicId = clinicId, Status = SubscriptionStatus.Trial }
        };

        var upgradedDetail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            ClinicId = clinicId,
            PackageId = newPackageId,
            Status = SubscriptionStatus.Trial
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsAsync(clinicId))
            .ReturnsAsync(Result<List<SubscriptionDto>>.Success(subscriptions));

        _modificationServiceMock
            .Setup(x => x.UpgradeSubscriptionAsync(subscriptionId, It.IsAny<UpgradeSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(upgradedDetail));

        // Act
        var result = await _sut.UpdateSubscription(clinicId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region ActivateWithCashPayment

    [Fact]
    public async Task ActivateWithCashPayment_ValidRequest_ReturnsOk()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var request = new ActivateCashRequest { PackageId = packageId };

        var subscriptionDetail = new SubscriptionDetailDto
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PackageId = packageId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        var mockProvider = new Mock<IPaymentProvider>();
        mockProvider.Setup(p => p.Currency).Returns("EGP");

        _lifecycleServiceMock
            .Setup(x => x.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(subscriptionDetail));

        _paymentServiceMock
            .Setup(x => x.GetProvider("cash"))
            .Returns(mockProvider.Object);

        _paymentServiceMock
            .Setup(x => x.RecordCashPaymentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), clinicId))
            .ReturnsAsync(Result<CashPaymentResult>.Success(new CashPaymentResult("TXN-123")));

        _clinicServiceMock
            .Setup(x => x.ActivateClinicAsync(clinicId))
            .ReturnsAsync(Result.Success());

        _clinicServiceMock
            .Setup(x => x.GetClinicByIdAsync(clinicId))
            .ReturnsAsync(Result<ClinicResponse>.Success(new ClinicResponse
            {
                Id = clinicId,
                Name = "Test Clinic",
                Email = "owner@test.com"
            }));

        _authPasswordServiceMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("reset-token");

        _authServiceMock
            .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ActivateWithCashPayment(clinicId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ActivateWithCashPayment_SubscriptionCreationFails_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new ActivateCashRequest { PackageId = Guid.NewGuid() };

        _lifecycleServiceMock
            .Setup(x => x.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Invalid package"));

        // Act
        var result = await _sut.ActivateWithCashPayment(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActivateWithCashPayment_PaymentRecordFails_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new ActivateCashRequest { PackageId = Guid.NewGuid() };

        var subscriptionDetail = new SubscriptionDetailDto
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active
        };

        var mockProvider = new Mock<IPaymentProvider>();
        mockProvider.Setup(p => p.Currency).Returns("EGP");

        _lifecycleServiceMock
            .Setup(x => x.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(subscriptionDetail));

        _paymentServiceMock
            .Setup(x => x.GetProvider("cash"))
            .Returns(mockProvider.Object);

        _paymentServiceMock
            .Setup(x => x.RecordCashPaymentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), clinicId))
            .ReturnsAsync(Result<CashPaymentResult>.Failure("Failed to record cash payment"));

        // Act
        var result = await _sut.ActivateWithCashPayment(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActivateWithCashPayment_ActivationFails_ReturnsBadRequest()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var request = new ActivateCashRequest { PackageId = Guid.NewGuid() };

        var subscriptionDetail = new SubscriptionDetailDto
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active
        };

        var mockProvider = new Mock<IPaymentProvider>();
        mockProvider.Setup(p => p.Currency).Returns("EGP");

        _lifecycleServiceMock
            .Setup(x => x.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequestDto>()))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(subscriptionDetail));

        _paymentServiceMock
            .Setup(x => x.GetProvider("cash"))
            .Returns(mockProvider.Object);

        _paymentServiceMock
            .Setup(x => x.RecordCashPaymentAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), clinicId))
            .ReturnsAsync(Result<CashPaymentResult>.Success(new CashPaymentResult("TXN-456")));

        _clinicServiceMock
            .Setup(x => x.ActivateClinicAsync(clinicId))
            .ReturnsAsync(Result.Failure("Cannot activate clinic"));

        // Act
        var result = await _sut.ActivateWithCashPayment(clinicId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
