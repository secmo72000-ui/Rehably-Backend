using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Communication;
using Rehably.Application.DTOs.Auth;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using Rehably.Infrastructure.Repositories;
using Rehably.Infrastructure.Services.Auth;
using Rehably.Tests.Helpers;

namespace Rehably.Tests.Services.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:AccessTokenExpirationMinutes"] = "1440",
                ["JwtSettings:MustChangePasswordTokenMinutes"] = "5",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _sut = new AuthService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _backgroundJobClientMock.Object,
            _configuration);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsError()
    {
        var result = await _sut.LoginAsync("nonexistent@example.com", "password");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ReturnsError()
    {
        var user = TestDataFactory.CreateTestUser(isActive: false);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);

        var result = await _sut.LoginAsync("test@example.com", "password");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account is disabled");
    }

    [Fact]
    public async Task LoginAsync_UserLocked_ReturnsErrorWithRemainingTime()
    {
        var lockoutEnd = DateTime.UtcNow.AddMinutes(10);
        var user = TestDataFactory.CreateTestUser(lockoutEnd: lockoutEnd);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync("test@example.com", "password");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Account is locked");
        result.Error.Should().Contain("minutes");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_IncrementsFailedCount()
    {
        var user = TestDataFactory.CreateTestUser(accessFailedCount: 3);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrongpassword")).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.LoginAsync("test@example.com", "wrongpassword");

        user.AccessFailedCount.Should().Be(4);
        result.Error.Should().Contain("1 attempts remaining");
    }

    [Fact]
    public async Task LoginAsync_FifthFailedAttempt_LocksAccountAndSendsEmail()
    {
        var user = TestDataFactory.CreateTestUser(accessFailedCount: 4);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrongpassword")).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _emailServiceMock.Setup(x => x.SendWithDefaultProviderAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new Application.DTOs.Communication.EmailResult { Success = true });

        var result = await _sut.LoginAsync("test@example.com", "wrongpassword");

        result.IsFailure.Should().BeTrue();
        user.AccessFailedCount.Should().Be(5);
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        result.Error.Should().Be("Account locked due to too many failed attempts. Try again in 15 minutes");
        _emailServiceMock.Verify(x => x.SendWithDefaultProviderAsync(
            It.Is<EmailMessage>(m => m.Subject.Contains("locked") && m.To == "test@example.com")),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ValidPasswordWithMustChangePassword_ReturnsTokenWithoutRefreshToken()
    {
        var user = TestDataFactory.CreateTestUser(mustChangePassword: true);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(x => x.GetPermissionsForRolesAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync(new HashSet<string>());
        _tokenServiceMock.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IList<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<bool>()))
            .ReturnsAsync("test-access-token");
        _tokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(user.Id, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync("test@example.com", "password");

        result.IsSuccess.Should().BeTrue();
        result.Data.AccessToken.Should().Be("test-access-token");
        result.Data.RefreshToken.Should().BeNull();
        result.Data.MustChangePassword.Should().BeTrue();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        _tokenServiceMock.Verify(x => x.GetPermissionsForRolesAsync(It.IsAny<IList<string>>()), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessTokenAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<IList<string>>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<bool>()), Times.Once);
        _tokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidPasswordWithoutMustChangePassword_ReturnsTokenWithRefreshToken()
    {
        var user = TestDataFactory.CreateTestUser(mustChangePassword: false);
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(x => x.GetPermissionsForRolesAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync(new HashSet<string>());
        _tokenServiceMock.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<IList<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<bool>()))
            .ReturnsAsync("test-access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");
        _tokenServiceMock.Setup(x => x.SaveRefreshTokenAsync(user.Id, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync("test@example.com", "password");

        result.IsSuccess.Should().BeTrue();
        result.Data.AccessToken.Should().Be("test-access-token");
        result.Data.RefreshToken.Should().Be("test-refresh-token");
        result.Data.MustChangePassword.Should().BeFalse();
        _tokenServiceMock.Verify(x => x.SaveRefreshTokenAsync(user.Id, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UnlockUserAsync_UserNotFound_ReturnsError()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.UnlockUserAsync("nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task UnlockUserAsync_UserNotLocked_ReturnsError()
    {
        var user = TestDataFactory.CreateTestUser();
        _userManagerMock.Setup(x => x.FindByIdAsync("test-user-id")).ReturnsAsync(user);

        var result = await _sut.UnlockUserAsync("test-user-id");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account is not currently locked");
    }

    [Fact]
    public async Task UnlockUserAsync_UserLocked_UnlocksAccountAndSendsEmail()
    {
        var user = TestDataFactory.CreateTestUser(accessFailedCount: 5, lockoutEnd: DateTime.UtcNow.AddMinutes(10));
        _userManagerMock.Setup(x => x.FindByIdAsync("test-user-id")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _emailServiceMock.Setup(x => x.SendWithDefaultProviderAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(new Application.DTOs.Communication.EmailResult { Success = true });

        var result = await _sut.UnlockUserAsync("test-user-id");

        result.IsSuccess.Should().BeTrue();
        user.LockoutEnd.Should().BeNull();
        user.AccessFailedCount.Should().Be(0);
        _emailServiceMock.Verify(x => x.SendWithDefaultProviderAsync(
            It.Is<EmailMessage>(m => m.Subject.Contains("unlocked") && m.To == "test@example.com")),
            Times.Once);
    }

    private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        return mock;
    }

    public void Dispose()
    {
    }
}
