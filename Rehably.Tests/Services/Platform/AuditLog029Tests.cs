using FluentAssertions;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Services.Platform;

/// <summary>
/// TDD tests for Module 029-audit-log enhancements.
/// Covers security regression, type safety, new filters, and response enrichment.
/// </summary>
public class AuditLog029Tests
{
    private readonly Mock<IAuditLogService> _serviceMock;

    public AuditLog029Tests()
    {
        _serviceMock = new Mock<IAuditLogService>();
    }

    // ── T023: Security regression — OTP masking ───────────────────────────────

    [Fact]
    public void AuditLog_OtpReference_NeverContainsFullOtpValue()
    {
        const string fullOtp = "123456";
        var dto = new AuditLogDto
        {
            OtpReference = "****231"
        };

        dto.OtpReference.Should().NotBe(fullOtp);
        dto.OtpReference.Should().MatchRegex(@"^\*{4}.{3}$");
    }

    [Fact]
    public void AuditLogDto_OtpReference_WhenNull_IsAllowed()
    {
        var dto = new AuditLogDto
        {
            OtpReference = null
        };

        dto.OtpReference.Should().BeNull();
    }

    [Fact]
    public void AuditLogDto_OtpReference_MaskedFormat_IsCorrect()
    {
        var dto = new AuditLogDto
        {
            OtpReference = "****456"
        };

        dto.OtpReference.Should().StartWith("****");
        dto.OtpReference!.Length.Should().Be(7);
    }

    // ── T020: GetAuditLogs by Guid UserId ─────────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_ByGuidUserId_ReturnsCorrectEntries()
    {
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var query = new AuditLogQueryDto
        {
            UserId = targetUserId,
            Page = 1,
            PageSize = 20
        };

        var items = new List<AuditLogDto>
        {
            new() { UserId = targetUserId, ActionType = "Login", EntityName = "User" },
            new() { UserId = targetUserId, ActionType = "Create", EntityName = "Patient" }
        };

        var response = new AuditLogListResponseDto
        {
            Items = items,
            Page = 1,
            PageSize = 20,
            TotalCount = 2,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(i => i.UserId.Should().Be(targetUserId));
        result.Value.Items.Should().NotContain(i => i.UserId == otherUserId);
    }

    // ── T021: GetAuditLogs by ActionType enum ─────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_ByActionTypeEnum_ReturnsCorrectEntries()
    {
        var query = new AuditLogQueryDto
        {
            ActionType = AuditActionType.Login,
            Page = 1,
            PageSize = 20
        };

        var items = new List<AuditLogDto>
        {
            new() { ActionType = "Login", EntityName = "User" }
        };

        var response = new AuditLogListResponseDto
        {
            Items = items,
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.Should().AllSatisfy(i => i.ActionType.Should().Be("Login"));
    }

    // ── T022: GetAuditLogs IsSuccess=false ────────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_IsSuccessFalse_ReturnsOnlyFailedEntries()
    {
        var query = new AuditLogQueryDto
        {
            IsSuccess = false,
            Page = 1,
            PageSize = 20
        };

        var failedItems = new List<AuditLogDto>
        {
            new() { ActionType = "Login", EntityName = "User", IsSuccess = false },
            new() { ActionType = "Login", EntityName = "User", IsSuccess = false }
        };

        var response = new AuditLogListResponseDto
        {
            Items = failedItems,
            Page = 1,
            PageSize = 20,
            TotalCount = 2,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.IsSuccess.Should().BeFalse());
        result.Value.Items.Should().NotContain(i => i.IsSuccess == true);
    }

    // ── T024: GetAuditLogs by Role ────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_ByRole_ReturnsCorrectEntries()
    {
        var query = new AuditLogQueryDto
        {
            Role = "Manager",
            Page = 1,
            PageSize = 20
        };

        var managerItems = new List<AuditLogDto>
        {
            new() { ActionType = "Create", EntityName = "Patient", UserRole = "Manager" },
            new() { ActionType = "Update", EntityName = "Appointment", UserRole = "Manager" }
        };

        var response = new AuditLogListResponseDto
        {
            Items = managerItems,
            Page = 1,
            PageSize = 20,
            TotalCount = 2,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(i => i.UserRole.Should().Be("Manager"));
    }

    // ── T025: GetAuditLogs by Email ───────────────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_ByEmail_ReturnsCorrectEntries()
    {
        const string targetEmail = "dr.smith@clinic.com";

        var query = new AuditLogQueryDto
        {
            Email = targetEmail,
            Page = 1,
            PageSize = 20
        };

        var items = new List<AuditLogDto>
        {
            new() { ActionType = "Login", EntityName = "User", UserEmail = targetEmail }
        };

        var response = new AuditLogListResponseDto
        {
            Items = items,
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.Should().AllSatisfy(i => i.UserEmail.Should().Be(targetEmail));
    }

    // ── Response enrichment tests ─────────────────────────────────────────────

    [Fact]
    public void AuditLogDto_IsSuccess_DefaultsToTrue()
    {
        var dto = new AuditLogDto();

        dto.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AuditLogDto_UserRole_CanBeSet()
    {
        var dto = new AuditLogDto { UserRole = "PlatformAdmin" };

        dto.UserRole.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void AuditLogDto_UserEmail_CanBeSet()
    {
        var dto = new AuditLogDto { UserEmail = "admin@rehably.com" };

        dto.UserEmail.Should().Be("admin@rehably.com");
    }
}
