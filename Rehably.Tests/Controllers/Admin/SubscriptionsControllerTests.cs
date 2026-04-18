using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using System.Security.Claims;

namespace Rehably.Tests.Controllers.Admin;

public class SubscriptionsControllerTests
{
    private readonly Mock<IPlatformSubscriptionService> _subscriptionServiceMock;
    private readonly Mock<ISubscriptionLifecycleService> _lifecycleServiceMock;
    private readonly Mock<ISubscriptionModificationService> _modificationServiceMock;
    private readonly Mock<ILogger<SubscriptionsController>> _loggerMock;
    private readonly SubscriptionsController _sut;

    public SubscriptionsControllerTests()
    {
        _subscriptionServiceMock = new Mock<IPlatformSubscriptionService>();
        _lifecycleServiceMock = new Mock<ISubscriptionLifecycleService>();
        _modificationServiceMock = new Mock<ISubscriptionModificationService>();
        _loggerMock = new Mock<ILogger<SubscriptionsController>>();

        _sut = new SubscriptionsController(
            _subscriptionServiceMock.Object,
            _lifecycleServiceMock.Object,
            _modificationServiceMock.Object,
            _loggerMock.Object);

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

    #region GetAll

    [Fact]
    public async Task GetAll_DefaultPagination_ReturnsOkWithPagedResult()
    {
        // Arrange
        var subscriptions = new List<SubscriptionDto>
        {
            new() { Id = Guid.NewGuid(), PackageName = "Pro", Status = SubscriptionStatus.Active },
            new() { Id = Guid.NewGuid(), PackageName = "Basic", Status = SubscriptionStatus.Trial }
        };
        var pagedResult = new PagedResult<SubscriptionDto>(subscriptions, 2, 1, 20);

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsPagedAsync(1, 20, null, null))
            .ReturnsAsync(Result<PagedResult<SubscriptionDto>>.Success(pagedResult));

        // Act
        var result = await _sut.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<SubscriptionDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var pagedResult = new PagedResult<SubscriptionDto>([], 0, 1, 20);

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsPagedAsync(1, 20, SubscriptionStatus.Active, null))
            .ReturnsAsync(Result<PagedResult<SubscriptionDto>>.Success(pagedResult));

        // Act
        var result = await _sut.GetAll(status: SubscriptionStatus.Active);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _subscriptionServiceMock.Verify(
            x => x.GetSubscriptionsPagedAsync(1, 20, SubscriptionStatus.Active, null), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithClinicIdFilter_PassesFilterToService()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var pagedResult = new PagedResult<SubscriptionDto>([], 0, 1, 20);

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsPagedAsync(1, 20, null, clinicId))
            .ReturnsAsync(Result<PagedResult<SubscriptionDto>>.Success(pagedResult));

        // Act
        var result = await _sut.GetAll(clinicId: clinicId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _subscriptionServiceMock.Verify(
            x => x.GetSubscriptionsPagedAsync(1, 20, null, clinicId), Times.Once);
    }

    [Fact]
    public async Task GetAll_InvalidPage_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetAll(page: 0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_InvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetAll(pageSize: -1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_PageSizeExceeds100_CapsAt100()
    {
        // Arrange
        var pagedResult = new PagedResult<SubscriptionDto>([], 0, 1, 100);

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionsPagedAsync(1, 100, null, null))
            .ReturnsAsync(Result<PagedResult<SubscriptionDto>>.Success(pagedResult));

        // Act
        var result = await _sut.GetAll(pageSize: 200);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _subscriptionServiceMock.Verify(
            x => x.GetSubscriptionsPagedAsync(1, 100, null, null), Times.Once);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new SubscriptionDto
        {
            Id = subscriptionId,
            PackageName = "Pro",
            Status = SubscriptionStatus.Active,
            ClinicId = Guid.NewGuid()
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionByIdAsync(subscriptionId))
            .ReturnsAsync(Result<SubscriptionDto>.Success(subscription));

        // Act
        var result = await _sut.GetById(subscriptionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SubscriptionDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(subscriptionId);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionByIdAsync(subscriptionId))
            .ReturnsAsync(Result<SubscriptionDto>.Failure("Subscription not found"));

        // Act
        var result = await _sut.GetById(subscriptionId);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetWithDetails

    [Fact]
    public async Task GetWithDetails_ExistingId_ReturnsOkWithDetails()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var detail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            PackageName = "Enterprise",
            Status = SubscriptionStatus.Active,
            FeatureUsage = [new SubscriptionFeatureUsageDto()]
        };

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionWithDetailsAsync(subscriptionId))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(detail));

        // Act
        var result = await _sut.GetWithDetails(subscriptionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SubscriptionDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(subscriptionId);
        response.Data.FeatureUsage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWithDetails_NonExistingId_Returns404()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _subscriptionServiceMock
            .Setup(x => x.GetSubscriptionWithDetailsAsync(subscriptionId))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Subscription not found"));

        // Act
        var result = await _sut.GetWithDetails(subscriptionId);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region Cancel

    [Fact]
    public async Task Cancel_ActiveSubscription_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new CancelSubscriptionRequestDto { Reason = "Clinic closing" };

        var cancelledDetail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            Status = SubscriptionStatus.Cancelled,
            CancelledAt = DateTime.UtcNow,
            CancelReason = "Clinic closing"
        };

        _lifecycleServiceMock
            .Setup(x => x.CancelSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(cancelledDetail));

        // Act
        var result = await _sut.Cancel(subscriptionId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SubscriptionDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsError()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new CancelSubscriptionRequestDto { Reason = "Again" };

        _lifecycleServiceMock
            .Setup(x => x.CancelSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Cannot cancel: subscription is already cancelled"));

        // Act
        var result = await _sut.Cancel(subscriptionId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Cancel_NonExistingSubscription_Returns404()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new CancelSubscriptionRequestDto { Reason = "Test" };

        _lifecycleServiceMock
            .Setup(x => x.CancelSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Subscription not found"));

        // Act
        var result = await _sut.Cancel(subscriptionId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region Renew

    [Fact]
    public async Task Renew_ValidRequest_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new RenewSubscriptionRequestDto { PackageId = Guid.NewGuid() };

        var renewedDetail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        _lifecycleServiceMock
            .Setup(x => x.RenewSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(renewedDetail));

        // Act
        var result = await _sut.Renew(subscriptionId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SubscriptionDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Renew_InvalidStatus_ReturnsError()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new RenewSubscriptionRequestDto { PackageId = Guid.NewGuid() };

        _lifecycleServiceMock
            .Setup(x => x.RenewSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Cannot renew: invalid subscription status"));

        // Act
        var result = await _sut.Renew(subscriptionId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Upgrade

    [Fact]
    public async Task Upgrade_ValidRequest_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new UpgradeSubscriptionRequestDto { NewPackageId = Guid.NewGuid() };

        var upgradedDetail = new SubscriptionDetailDto
        {
            Id = subscriptionId,
            PackageId = request.NewPackageId,
            Status = SubscriptionStatus.Active,
            PackageName = "Enterprise"
        };

        _modificationServiceMock
            .Setup(x => x.UpgradeSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Success(upgradedDetail));

        // Act
        var result = await _sut.Upgrade(subscriptionId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SubscriptionDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.PackageName.Should().Be("Enterprise");
    }

    [Fact]
    public async Task Upgrade_DowngradeAttempt_ReturnsError()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var request = new UpgradeSubscriptionRequestDto { NewPackageId = Guid.NewGuid() };

        _modificationServiceMock
            .Setup(x => x.UpgradeSubscriptionAsync(subscriptionId, request))
            .ReturnsAsync(Result<SubscriptionDetailDto>.Failure("Cannot downgrade subscription"));

        // Act
        var result = await _sut.Upgrade(subscriptionId, request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region ResetUsage

    [Fact]
    public async Task ResetUsage_ValidRequest_ReturnsOk()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        _lifecycleServiceMock
            .Setup(x => x.ResetUsageAsync(subscriptionId, featureId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.ResetUsage(subscriptionId, featureId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetUsage_InvalidSubscription_ReturnsError()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        _lifecycleServiceMock
            .Setup(x => x.ResetUsageAsync(subscriptionId, featureId))
            .ReturnsAsync(Result.Failure("Subscription not found"));

        // Act
        var result = await _sut.ResetUsage(subscriptionId, featureId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ResetUsage_InvalidFeature_ReturnsError()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        _lifecycleServiceMock
            .Setup(x => x.ResetUsageAsync(subscriptionId, featureId))
            .ReturnsAsync(Result.Failure("Feature not found in subscription"));

        // Act
        var result = await _sut.ResetUsage(subscriptionId, featureId);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion
}
