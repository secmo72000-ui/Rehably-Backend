using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Auth;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.Services.Auth;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Auth;

public class OtpControllerTests
{
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IAuthOtpService> _authOtpServiceMock;
    private readonly Mock<IAuthPasswordService> _authPasswordServiceMock;
    private readonly OtpController _sut;

    public OtpControllerTests()
    {
        _otpServiceMock = new Mock<IOtpService>();
        _authOtpServiceMock = new Mock<IAuthOtpService>();
        _authPasswordServiceMock = new Mock<IAuthPasswordService>();
        _sut = new OtpController(
            _otpServiceMock.Object,
            _authOtpServiceMock.Object,
            _authPasswordServiceMock.Object);
        SetupHttpContext();
    }

    #region RequestOtpLogin

    [Fact]
    public async Task RequestOtpLogin_ValidEmail_ReturnsOk()
    {
        _authOtpServiceMock
            .Setup(x => x.RequestOtpLoginAsync("user@example.com"))
            .ReturnsAsync(Result.Success());

        var request = new RequestOtpLoginDto { Email = "user@example.com" };
        var result = await _sut.RequestOtpLogin(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RequestOtpLogin_ServiceFails_ReturnsError()
    {
        _authOtpServiceMock
            .Setup(x => x.RequestOtpLoginAsync("user@example.com"))
            .ReturnsAsync(Result.Failure("User not found"));

        var request = new RequestOtpLoginDto { Email = "user@example.com" };
        var result = await _sut.RequestOtpLogin(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region VerifyOtpLogin

    [Fact]
    public async Task VerifyOtpLogin_ValidOtp_ReturnsOkWithTokens()
    {
        var loginResponse = new LoginResponseDto
        {
            AccessToken = "otp-access-token",
            RefreshToken = "otp-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            MustChangePassword = false,
            EmailVerified = true
        };
        _authOtpServiceMock
            .Setup(x => x.VerifyOtpLoginAsync("user@example.com", "123456"))
            .ReturnsAsync(Result<LoginResponseDto>.Success(loginResponse));

        var request = new VerifyOtpLoginDto { Email = "user@example.com", OtpCode = "123456" };
        var result = await _sut.VerifyOtpLogin(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().Be("otp-access-token");
    }

    [Fact]
    public async Task VerifyOtpLogin_InvalidOtp_ReturnsError()
    {
        _authOtpServiceMock
            .Setup(x => x.VerifyOtpLoginAsync("user@example.com", "000000"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("Invalid OTP code"));

        var request = new VerifyOtpLoginDto { Email = "user@example.com", OtpCode = "000000" };
        var result = await _sut.VerifyOtpLogin(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task VerifyOtpLogin_ExpiredOtp_ReturnsError()
    {
        _authOtpServiceMock
            .Setup(x => x.VerifyOtpLoginAsync("user@example.com", "123456"))
            .ReturnsAsync(Result<LoginResponseDto>.Failure("OTP has expired"));

        var request = new VerifyOtpLoginDto { Email = "user@example.com", OtpCode = "123456" };
        var result = await _sut.VerifyOtpLogin(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region RequestOtp

    [Fact]
    public async Task RequestOtp_LoginPurpose_CallsOtpServiceAndReturnsOk()
    {
        _otpServiceMock
            .Setup(x => x.GenerateOtpAsync("user@example.com", OtpPurpose.Login))
            .ReturnsAsync(Result<string>.Success("123456"));

        var request = new OtpRequestDto { Email = "user@example.com", Purpose = "login" };
        var result = await _sut.RequestOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
        _otpServiceMock.Verify(x => x.GenerateOtpAsync("user@example.com", OtpPurpose.Login), Times.Once);
    }

    [Fact]
    public async Task RequestOtp_PasswordResetPurpose_CallsPasswordServiceAndReturnsOk()
    {
        _authPasswordServiceMock
            .Setup(x => x.RequestOtpPasswordResetAsync("user@example.com", It.IsAny<string>()))
            .ReturnsAsync(Result.Success());

        var request = new OtpRequestDto { Email = "user@example.com", Purpose = "password_reset" };
        var result = await _sut.RequestOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _authPasswordServiceMock.Verify(
            x => x.RequestOtpPasswordResetAsync("user@example.com", It.IsAny<string>()), Times.Once);
        _otpServiceMock.Verify(
            x => x.GenerateOtpAsync(It.IsAny<string>(), It.IsAny<OtpPurpose>()), Times.Never);
    }

    [Fact]
    public async Task RequestOtp_InvalidPurpose_ReturnsValidationError()
    {
        var request = new OtpRequestDto { Email = "user@example.com", Purpose = "invalid_purpose" };
        var result = await _sut.RequestOtp(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region VerifyOtp

    [Fact]
    public async Task VerifyOtp_PasswordResetPurpose_ReturnsResetToken()
    {
        var tokenResult = new PasswordResetTokenResult
        {
            ResetToken = "reset-token-abc",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        _authPasswordServiceMock
            .Setup(x => x.VerifyOtpPasswordResetAsync("user@example.com", "123456"))
            .ReturnsAsync(Result<PasswordResetTokenResult>.Success(tokenResult));

        var request = new OtpVerifyDto { Email = "user@example.com", Code = "123456", Purpose = "password_reset" };
        var result = await _sut.VerifyOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<OtpVerifyResponseDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.ResetToken.Should().Be("reset-token-abc");
        response.Data.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyOtp_PasswordResetInvalidCode_ReturnsInvalidOtpError()
    {
        _authPasswordServiceMock
            .Setup(x => x.VerifyOtpPasswordResetAsync("user@example.com", "000000"))
            .ReturnsAsync(Result<PasswordResetTokenResult>.Failure("Invalid verification code"));

        var request = new OtpVerifyDto { Email = "user@example.com", Code = "000000", Purpose = "password_reset" };
        var result = await _sut.VerifyOtp(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
        var response = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Error!.Code.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task VerifyOtp_LoginPurposeValid_ReturnsCodeVerified()
    {
        var verifyResult = new OtpVerifyResult { IsValid = true, AttemptsRemaining = 3, UserId = "user-123" };
        _otpServiceMock
            .Setup(x => x.VerifyOtpAsync("user@example.com", "123456", OtpPurpose.Login))
            .ReturnsAsync(Result<OtpVerifyResult>.Success(verifyResult));

        var request = new OtpVerifyDto { Email = "user@example.com", Code = "123456", Purpose = "login" };
        var result = await _sut.VerifyOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyOtp_LoginPurposeInvalid_ReturnsInvalidOtpError()
    {
        var verifyResult = new OtpVerifyResult { IsValid = false, AttemptsRemaining = 2, UserId = null };
        _otpServiceMock
            .Setup(x => x.VerifyOtpAsync("user@example.com", "000000", OtpPurpose.Login))
            .ReturnsAsync(Result<OtpVerifyResult>.Success(verifyResult));

        var request = new OtpVerifyDto { Email = "user@example.com", Code = "000000", Purpose = "login" };
        var result = await _sut.VerifyOtp(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
        var response = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Error!.Code.Should().Be("INVALID_OTP");
    }

    [Fact]
    public async Task VerifyOtp_InvalidPurpose_ReturnsValidationError()
    {
        var request = new OtpVerifyDto { Email = "user@example.com", Code = "123456", Purpose = "unknown" };
        var result = await _sut.VerifyOtp(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ResendOtp

    [Fact]
    public async Task ResendOtp_ValidRequest_ReturnsOk()
    {
        _otpServiceMock
            .Setup(x => x.ResendOtpAsync("user@example.com", OtpPurpose.Login))
            .ReturnsAsync(Result.Success());

        var request = new OtpResendDto { Email = "user@example.com", Purpose = "login" };
        var result = await _sut.ResendOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResendOtp_CooldownActive_Returns429()
    {
        _otpServiceMock
            .Setup(x => x.ResendOtpAsync("user@example.com", OtpPurpose.Login))
            .ReturnsAsync(Result.Failure("Please wait 45 seconds before requesting a new code"));

        var request = new OtpResendDto { Email = "user@example.com", Purpose = "login" };
        var result = await _sut.ResendOtp(request);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(429);
        var response = objectResult.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Error!.Code.Should().Be("RATE_LIMITED");
    }

    [Fact]
    public async Task ResendOtp_InvalidPurpose_ReturnsValidationError()
    {
        var request = new OtpResendDto { Email = "user@example.com", Purpose = "bad_value" };
        var result = await _sut.ResendOtp(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResendOtp_PasswordResetPurpose_ReturnsOk()
    {
        _otpServiceMock
            .Setup(x => x.ResendOtpAsync("user@example.com", OtpPurpose.PasswordReset))
            .ReturnsAsync(Result.Success());

        var request = new OtpResendDto { Email = "user@example.com", Purpose = "password_reset" };
        var result = await _sut.ResendOtp(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _otpServiceMock.Verify(x => x.ResendOtpAsync("user@example.com", OtpPurpose.PasswordReset), Times.Once);
    }

    #endregion

    #region Helpers

    private void SetupHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #endregion
}
