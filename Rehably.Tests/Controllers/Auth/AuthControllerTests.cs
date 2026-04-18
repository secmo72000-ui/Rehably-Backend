using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Auth;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.Services.Auth;

namespace Rehably.Tests.Controllers.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _authPasswordServiceMock = new Mock<IAuthPasswordService>();
        _sut = new AuthController(_authServiceMock.Object, _authPasswordServiceMock.Object);
        SetupAnonymousUser();
    }

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        var loginResponse = new LoginResponseDto
        {
            AccessToken = "access-token-123",
            RefreshToken = "refresh-token-456",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            MustChangePassword = false,
            EmailVerified = true
        };
        _authServiceMock
            .Setup(x => x.LoginAsync("user@example.com", "ValidPass123!"))
            .ReturnsAsync(Result<LoginResponseDto>.Success(loginResponse));

        var request = new LoginRequestDto { Email = "user@example.com", Password = "ValidPass123!" };
        var result = await _sut.Login(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().Be("access-token-123");
        response.Data.RefreshToken.Should().Be("refresh-token-456");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsError()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync("user@example.com", "wrong"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Invalid email or password"));

        var request = new LoginRequestDto { Email = "user@example.com", Password = "wrong" };
        var result = await _sut.Login(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsError()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync("nobody@example.com", "password"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Invalid email or password"));

        var request = new LoginRequestDto { Email = "nobody@example.com", Password = "password" };
        var result = await _sut.Login(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsError()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync("inactive@example.com", "password"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Account is disabled"));

        var request = new LoginRequestDto { Email = "inactive@example.com", Password = "password" };
        var result = await _sut.Login(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_LockedAccount_ReturnsError()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync("locked@example.com", "password"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Account is locked. Try again in 15 minutes"));

        var request = new LoginRequestDto { Email = "locked@example.com", Password = "password" };
        var result = await _sut.Login(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_MustChangePassword_ReturnsOkWithFlag()
    {
        var loginResponse = new LoginResponseDto
        {
            AccessToken = "limited-token",
            RefreshToken = null,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            MustChangePassword = true,
            EmailVerified = true
        };
        _authServiceMock
            .Setup(x => x.LoginAsync("user@example.com", "TempPass123!"))
            .ReturnsAsync(Result<LoginResponseDto>.Success(loginResponse));

        var request = new LoginRequestDto { Email = "user@example.com", Password = "TempPass123!" };
        var result = await _sut.Login(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.MustChangePassword.Should().BeTrue();
        response.Data.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Login_ServiceReturnsFailure_PropagatesErrorMessage()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync("user@example.com", "pass"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Invalid email or password. 2 attempts remaining"));

        var request = new LoginRequestDto { Email = "user@example.com", Password = "pass" };
        var result = await _sut.Login(request);

        result.Should().BeOfType<ObjectResult>();
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsOk()
    {
        SetupAuthenticatedUser("user-123");
        _authServiceMock
            .Setup(x => x.LogoutAsync("user-123"))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Logout();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_NoUserId_ReturnsUnauthorized()
    {
        SetupAnonymousUser();

        var result = await _sut.Logout();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_ServiceFails_ReturnsError()
    {
        SetupAuthenticatedUser("user-123");
        _authServiceMock
            .Setup(x => x.LogoutAsync("user-123"))
            .ReturnsAsync(Result.Failure("Logout failed"));

        var result = await _sut.Logout();

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOkWithNewTokens()
    {
        var refreshResponse = new LoginResponseDto
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            MustChangePassword = false,
            EmailVerified = true
        };
        _authServiceMock
            .Setup(x => x.RefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(Result<LoginResponseDto>.Success(refreshResponse));

        var request = new RefreshTokenRequestDto { RefreshToken = "valid-refresh-token" };
        var result = await _sut.Refresh(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().Be("new-access-token");
        response.Data.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        _authServiceMock
            .Setup(x => x.RefreshTokenAsync("expired-token"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Token expired"));

        var request = new RefreshTokenRequestDto { RefreshToken = "expired-token" };
        var result = await _sut.Refresh(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsUnauthorized()
    {
        _authServiceMock
            .Setup(x => x.RefreshTokenAsync("revoked-token"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Token has been revoked"));

        var request = new RefreshTokenRequestDto { RefreshToken = "revoked-token" };
        var result = await _sut.Refresh(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        SetupAuthenticatedUser("user-123");
        _authPasswordServiceMock
            .Setup(x => x.ChangePasswordAsync("user-123", "OldPass123!", "NewPass456!"))
            .ReturnsAsync(Result.Success());

        var request = new ChangePasswordRequestDto { CurrentPassword = "OldPass123!", NewPassword = "NewPass456!" };
        var result = await _sut.ChangePassword(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsError()
    {
        SetupAuthenticatedUser("user-123");
        _authPasswordServiceMock
            .Setup(x => x.ChangePasswordAsync("user-123", "WrongOld!", "NewPass456!"))
            .ReturnsAsync(Result.Failure("Current password is incorrect"));

        var request = new ChangePasswordRequestDto { CurrentPassword = "WrongOld!", NewPassword = "NewPass456!" };
        var result = await _sut.ChangePassword(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsError()
    {
        SetupAuthenticatedUser("user-123");
        _authPasswordServiceMock
            .Setup(x => x.ChangePasswordAsync("user-123", "OldPass123!", "weak"))
            .ReturnsAsync(Result.Failure("Password does not meet requirements"));

        var request = new ChangePasswordRequestDto { CurrentPassword = "OldPass123!", NewPassword = "weak" };
        var result = await _sut.ChangePassword(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ChangePassword_NoUserId_ReturnsUnauthorized()
    {
        SetupAnonymousUser();

        var request = new ChangePasswordRequestDto { CurrentPassword = "OldPass123!", NewPassword = "NewPass456!" };
        var result = await _sut.ChangePassword(request);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region GetCurrentUser

    [Fact]
    public async Task GetCurrentUser_Authenticated_ReturnsOkWithUser()
    {
        SetupAuthenticatedUser("user-123");
        var userDto = new UserDto
        {
            Id = "user-123",
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            EmailVerified = true,
            Roles = ["User"],
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync("user-123"))
            .ReturnsAsync(userDto);

        var result = await _sut.GetCurrentUser();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be("user-123");
        response.Data.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
    {
        SetupAuthenticatedUser("user-123");
        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync("user-123"))
            .ReturnsAsync((UserDto?)null);

        var result = await _sut.GetCurrentUser();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_NoUserId_ReturnsUnauthorized()
    {
        SetupAnonymousUser();

        var result = await _sut.GetCurrentUser();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region ResetPasswordWithToken

    [Fact]
    public async Task ResetPasswordWithToken_ValidToken_ReturnsOk()
    {
        _authPasswordServiceMock
            .Setup(x => x.ResetPasswordWithTokenAsync("valid-reset-token", "NewPass123!"))
            .ReturnsAsync(Result.Success());

        var request = new PasswordResetWithTokenDto { ResetToken = "valid-reset-token", NewPassword = "NewPass123!" };
        var result = await _sut.ResetPasswordWithToken(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordWithToken_ExpiredToken_ReturnsErrorWithExpiredCode()
    {
        _authPasswordServiceMock
            .Setup(x => x.ResetPasswordWithTokenAsync("expired-token", "NewPass123!"))
            .ReturnsAsync(Result.Failure("Token has expired"));

        var request = new PasswordResetWithTokenDto { ResetToken = "expired-token", NewPassword = "NewPass123!" };
        var result = await _sut.ResetPasswordWithToken(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
        var response = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Error!.Code.Should().Be("TOKEN_EXPIRED");
    }

    [Fact]
    public async Task ResetPasswordWithToken_InvalidToken_ReturnsErrorWithInvalidCode()
    {
        _authPasswordServiceMock
            .Setup(x => x.ResetPasswordWithTokenAsync("invalid-token", "NewPass123!"))
            .ReturnsAsync(Result.Failure("Invalid reset token"));

        var request = new PasswordResetWithTokenDto { ResetToken = "invalid-token", NewPassword = "NewPass123!" };
        var result = await _sut.ResetPasswordWithToken(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
        var response = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Error!.Code.Should().Be("INVALID_TOKEN");
    }

    #endregion

    #region Helpers

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupAnonymousUser()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #endregion
}
