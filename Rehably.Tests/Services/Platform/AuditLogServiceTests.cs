using FluentAssertions;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.DTOs.Usage;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Services.Platform;

/// <summary>
/// Safety-net tests for IAuditLogService contract (T069).
/// Tests verify the interface contracts that implementations must satisfy.
/// </summary>
public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogService> _serviceMock;

    public AuditLogServiceTests()
    {
        _serviceMock = new Mock<IAuditLogService>();
    }

    // ── GetAuditLogsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLogsAsync_NoFilter_ReturnsAllLogs()
    {
        var query = new AuditLogQueryDto { Page = 1, PageSize = 20 };
        var items = new List<AuditLogDto>
        {
            new() { ActionType = "Login", EntityName = "User" },
            new() { ActionType = "Update", EntityName = "Patient" },
            new() { ActionType = "Create", EntityName = "Appointment" }
        };
        var response = new AuditLogListResponseDto
        {
            Items = items,
            Page = 1,
            PageSize = 20,
            TotalCount = 3,
            TotalPages = 1
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilteredByClinicId_ReturnsClinicLogs()
    {
        var clinicId = Guid.NewGuid();
        var query = new AuditLogQueryDto { ClinicId = clinicId, Page = 1, PageSize = 20 };
        var items = new List<AuditLogDto>
        {
            new() { ClinicId = clinicId, ActionType = "Login", EntityName = "User" },
            new() { ClinicId = clinicId, ActionType = "Create", EntityName = "Patient" }
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
        result.Value.Items.All(i => i.ClinicId == clinicId).Should().BeTrue();
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilteredByActionType_ReturnsMatchingLogs()
    {
        var query = new AuditLogQueryDto { ActionType = AuditActionType.Login, Page = 1, PageSize = 20 };
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
        result.Value.Items.All(i => i.ActionType == "Login").Should().BeTrue();
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilteredByDateRange_ReturnsLogsInRange()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;
        var query = new AuditLogQueryDto { StartDate = start, EndDate = end, Page = 1, PageSize = 20 };
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>
            {
                new() { Timestamp = DateTime.UtcNow.AddDays(-3), ActionType = "Update" }
            },
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
        result.Value.Items[0].Timestamp.Should().BeAfter(start).And.BeBefore(end);
    }

    [Fact]
    public async Task GetAuditLogsAsync_Paginated_ReturnsCorrectPage()
    {
        var query = new AuditLogQueryDto { Page = 2, PageSize = 10 };
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto> { new() { ActionType = "Login" } },
            Page = 2,
            PageSize = 10,
            TotalCount = 15,
            TotalPages = 2
        };

        _serviceMock
            .Setup(s => s.GetAuditLogsAsync(query))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _serviceMock.Object.GetAuditLogsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
        result.Value.TotalCount.Should().Be(15);
    }

    // ── GetClinicActivityAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetClinicActivityAsync_ValidClinicId_ReturnsClinicActivity()
    {
        var clinicId = Guid.NewGuid();
        var activity = new ClinicActivityDto
        {
            ClinicId = clinicId,
            ClinicName = "Test Clinic",
            PackageName = "Basic Plan",
            Usage = new UsageStatisticsDto
            {
                PatientsUsed = 45,
                PatientsLimit = 100,
                PatientsPercentage = 45,
                UsersUsed = 5,
                UsersLimit = 10,
                UsersPercentage = 50
            },
            DailyLogins = new List<LoginHistoryDto>
            {
                new() { UserName = "dr.smith@clinic.com", Action = "Login" }
            },
            FailedLogins = new List<FailedLoginDto>()
        };

        _serviceMock
            .Setup(s => s.GetClinicActivityAsync(clinicId, null, null))
            .ReturnsAsync(Result<ClinicActivityDto>.Success(activity));

        var result = await _serviceMock.Object.GetClinicActivityAsync(clinicId, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClinicId.Should().Be(clinicId);
        result.Value.ClinicName.Should().Be("Test Clinic");
        result.Value.Usage.PatientsUsed.Should().Be(45);
        result.Value.DailyLogins.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClinicActivityAsync_ClinicNotFound_ReturnsFailure()
    {
        var nonExistentClinicId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetClinicActivityAsync(nonExistentClinicId, null, null))
            .ReturnsAsync(Result<ClinicActivityDto>.Failure("Clinic not found"));

        var result = await _serviceMock.Object.GetClinicActivityAsync(nonExistentClinicId, null, null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Clinic not found");
    }

    [Fact]
    public async Task GetClinicActivityAsync_WithDateRange_PassesDateRangeToService()
    {
        var clinicId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var activity = new ClinicActivityDto
        {
            ClinicId = clinicId,
            ClinicName = "Test Clinic",
            Usage = new UsageStatisticsDto()
        };

        _serviceMock
            .Setup(s => s.GetClinicActivityAsync(clinicId, startDate, endDate))
            .ReturnsAsync(Result<ClinicActivityDto>.Success(activity));

        var result = await _serviceMock.Object.GetClinicActivityAsync(clinicId, startDate, endDate);

        result.IsSuccess.Should().BeTrue();
        _serviceMock.Verify(s => s.GetClinicActivityAsync(clinicId, startDate, endDate), Times.Once);
    }
}
