using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.Tests.Controllers.Admin;

public class AdminStagesControllerTests
{
    private readonly Mock<ITreatmentStageService> _stageServiceMock;
    private readonly AdminStagesController _sut;

    public AdminStagesControllerTests()
    {
        _stageServiceMock = new Mock<ITreatmentStageService>();
        _sut = new AdminStagesController(_stageServiceMock.Object);
    }

    #region GetAll

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = new LibraryItemListResponse<TreatmentStageDto>
        {
            Items = new List<TreatmentStageDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Acute Phase", Code = "STG001" },
                new() { Id = Guid.NewGuid(), Name = "Recovery Phase", Code = "STG002" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _stageServiceMock
            .Setup(x => x.GetStagesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentStageDto>>.Success(response));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsOk()
    {
        var response = new LibraryItemListResponse<TreatmentStageDto>
        {
            Items = new List<TreatmentStageDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _stageServiceMock
            .Setup(x => x.GetStagesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentStageDto>>.Success(response));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ServiceFails_ReturnsError()
    {
        _stageServiceMock
            .Setup(x => x.GetStagesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentStageDto>>.Failure("Failed to retrieve stages"));

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var stageId = Guid.NewGuid();
        var stage = new TreatmentStageDto
        {
            Id = stageId,
            Name = "Acute Phase",
            Code = "STG001",
            MinWeeks = 1,
            MaxWeeks = 4,
            MinSessions = 3,
            MaxSessions = 12
        };

        _stageServiceMock
            .Setup(x => x.GetStageByIdAsync(stageId))
            .ReturnsAsync(Result<TreatmentStageDto>.Success(stage));

        var result = await _sut.GetById(stageId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var stageId = Guid.NewGuid();

        _stageServiceMock
            .Setup(x => x.GetStageByIdAsync(stageId))
            .ReturnsAsync(Result<TreatmentStageDto>.Failure("Stage not found"));

        var result = await _sut.GetById(stageId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        var request = new CreateTreatmentStageRequest
        {
            Name = "New Stage",
            Code = "STG003",
            BodyRegionId = Guid.NewGuid(),
            MinWeeks = 2,
            MaxWeeks = 6,
            MinSessions = 6,
            MaxSessions = 18
        };

        var created = new TreatmentStageDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            BodyRegionId = request.BodyRegionId,
            MinWeeks = request.MinWeeks,
            MaxWeeks = request.MaxWeeks,
            MinSessions = request.MinSessions,
            MaxSessions = request.MaxSessions
        };

        _stageServiceMock
            .Setup(x => x.CreateStageAsync(request, Guid.Empty))
            .ReturnsAsync(Result<TreatmentStageDto>.Success(created));

        var result = await _sut.Create(request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_Invalid_Returns400()
    {
        var request = new CreateTreatmentStageRequest
        {
            Name = "New Stage",
            Code = "STG003",
            BodyRegionId = Guid.NewGuid()
        };

        _stageServiceMock
            .Setup(x => x.CreateStageAsync(request, Guid.Empty))
            .ReturnsAsync(Result<TreatmentStageDto>.Failure("Duplicate code"));

        var result = await _sut.Create(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_Found_ReturnsOk()
    {
        var stageId = Guid.NewGuid();

        _stageServiceMock
            .Setup(x => x.DeleteStageAsync(stageId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Delete(stageId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var stageId = Guid.NewGuid();

        _stageServiceMock
            .Setup(x => x.DeleteStageAsync(stageId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Stage not found"));

        var result = await _sut.Delete(stageId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
