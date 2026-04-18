using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AuditLogsControllerTests
{
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly AuditLogsController _sut;

    public AuditLogsControllerTests()
    {
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _sut = new AuditLogsController(_auditLogServiceMock.Object);
    }

    #region GetAuditLogs

    [Fact]
    public async Task GetAuditLogs_DefaultParams_ReturnsOkWithPagedList()
    {
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>
            {
                new() { Id = Guid.NewGuid(), ActionType = "Login", IsSuccess = true }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q =>
                q.Page == 1 && q.PageSize == 20 &&
                q.ClinicId == null && q.UserId == null && q.ActionType == null)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLogs_FilterByClinicId_PassesFilterToService()
    {
        var clinicId = Guid.NewGuid();
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q => q.ClinicId == clinicId)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs(clinicId: clinicId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLogs_FilterByUserId_PassesFilterToService()
    {
        var userId = Guid.NewGuid();
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q => q.UserId == userId)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs(userId: userId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLogs_FilterByActionType_PassesFilterToService()
    {
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q => q.ActionType == AuditActionType.Login)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs(actionType: AuditActionType.Login);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLogs_FilterByDateRange_PassesFilterToService()
    {
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q =>
                q.StartDate == startDate && q.EndDate == endDate)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs(startDate: startDate, endDate: endDate);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLogs_PageSizeExceeds100_ClampedTo100()
    {
        var response = new AuditLogListResponseDto
        {
            Items = new List<AuditLogDto>(),
            Page = 1,
            PageSize = 100,
            TotalCount = 0,
            TotalPages = 0
        };
        _auditLogServiceMock
            .Setup(x => x.GetAuditLogsAsync(It.Is<AuditLogQueryDto>(q => q.PageSize == 100)))
            .ReturnsAsync(Result<AuditLogListResponseDto>.Success(response));

        var result = await _sut.GetAuditLogs(pageSize: 200);

        result.Result.Should().BeOfType<OkObjectResult>();
        _auditLogServiceMock.Verify(x => x.GetAuditLogsAsync(
            It.Is<AuditLogQueryDto>(q => q.PageSize == 100)), Times.Once);
    }

    #endregion

    #region GetClinicActivity

    [Fact]
    public async Task GetClinicActivity_WhenFound_ReturnsOk()
    {
        var clinicId = Guid.NewGuid();
        var activity = new ClinicActivityDto
        {
            ClinicId = clinicId,
            ClinicName = "Test Clinic",
            PackageName = "Basic"
        };
        _auditLogServiceMock
            .Setup(x => x.GetClinicActivityAsync(clinicId, null, null))
            .ReturnsAsync(Result<ClinicActivityDto>.Success(activity));

        var result = await _sut.GetClinicActivity(clinicId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetClinicActivity_WhenNotFound_Returns404()
    {
        var clinicId = Guid.NewGuid();
        _auditLogServiceMock
            .Setup(x => x.GetClinicActivityAsync(clinicId, null, null))
            .ReturnsAsync(Result<ClinicActivityDto>.Failure("Clinic not found"));

        var result = await _sut.GetClinicActivity(clinicId);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetClinicActivity_WithDateRange_PassesDatesToService()
    {
        var clinicId = Guid.NewGuid();
        var startDate = new DateTime(2026, 2, 1);
        var endDate = new DateTime(2026, 2, 28);
        var activity = new ClinicActivityDto
        {
            ClinicId = clinicId,
            ClinicName = "Test Clinic"
        };
        _auditLogServiceMock
            .Setup(x => x.GetClinicActivityAsync(clinicId, startDate, endDate))
            .ReturnsAsync(Result<ClinicActivityDto>.Success(activity));

        var result = await _sut.GetClinicActivity(clinicId, startDate, endDate);

        result.Result.Should().BeOfType<OkObjectResult>();
        _auditLogServiceMock.Verify(x => x.GetClinicActivityAsync(clinicId, startDate, endDate), Times.Once);
    }

    #endregion
}
