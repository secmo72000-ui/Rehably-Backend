using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AdminExercisesControllerTests
{
    private readonly Mock<IExerciseService> _exerciseServiceMock;
    private readonly AdminExercisesController _sut;

    public AdminExercisesControllerTests()
    {
        _exerciseServiceMock = new Mock<IExerciseService>();
        _sut = new AdminExercisesController(_exerciseServiceMock.Object);
    }

    #region GetExercises

    [Fact]
    public async Task GetExercises_WhenServiceSucceeds_ReturnsOkWithPaginatedList()
    {
        var response = new LibraryItemListResponse<ExerciseDto>
        {
            Items = new List<ExerciseDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Shoulder Press", BodyRegionCategoryId = Guid.NewGuid() },
                new() { Id = Guid.NewGuid(), Name = "Knee Extension", BodyRegionCategoryId = Guid.NewGuid() }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _exerciseServiceMock
            .Setup(x => x.GetExercisesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ExerciseDto>>.Success(response));

        var result = await _sut.GetExercises();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetExercises_WithBodyRegionFilter_PassesFilterToService()
    {
        var bodyRegionId = Guid.NewGuid();
        var response = new LibraryItemListResponse<ExerciseDto>
        {
            Items = new List<ExerciseDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _exerciseServiceMock
            .Setup(x => x.GetExercisesAsync(bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ExerciseDto>>.Success(response));

        var result = await _sut.GetExercises(bodyRegionId);

        result.Result.Should().BeOfType<OkObjectResult>();
        _exerciseServiceMock.Verify(x => x.GetExercisesAsync(bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetExercises_WhenServiceFails_ReturnsBadRequest()
    {
        _exerciseServiceMock
            .Setup(x => x.GetExercisesAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ExerciseDto>>.Failure("Failed to retrieve exercises"));

        var result = await _sut.GetExercises();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetExercise

    [Fact]
    public async Task GetExercise_WhenFound_ReturnsOk()
    {
        var exerciseId = Guid.NewGuid();
        var exercise = new ExerciseDto
        {
            Id = exerciseId,
            Name = "Shoulder Press",
            BodyRegionCategoryId = Guid.NewGuid(),
            Repeats = 10,
            Steps = 3,
            AccessTier = LibraryAccessTier.Free
        };

        _exerciseServiceMock
            .Setup(x => x.GetExerciseByIdAsync(exerciseId))
            .ReturnsAsync(Result<ExerciseDto>.Success(exercise));

        var result = await _sut.GetExercise(exerciseId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(exercise);
    }

    [Fact]
    public async Task GetExercise_WhenNotFound_ReturnsNotFound()
    {
        var exerciseId = Guid.NewGuid();

        _exerciseServiceMock
            .Setup(x => x.GetExerciseByIdAsync(exerciseId))
            .ReturnsAsync(Result<ExerciseDto>.Failure("Exercise not found"));

        var result = await _sut.GetExercise(exerciseId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateExercise

    [Fact]
    public async Task CreateExercise_WhenValid_ReturnsCreatedAtAction()
    {
        var request = new CreateExerciseRequest
        {
            Name = "New Exercise",
            BodyRegionCategoryId = Guid.NewGuid(),
            Repeats = 12,
            Steps = 3
        };

        var created = new ExerciseDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            Repeats = request.Repeats,
            ClinicId = null
        };

        _exerciseServiceMock
            .Setup(x => x.CreateExerciseAsync(It.Is<CreateExerciseRequest>(r => r.Name == request.Name), null))
            .ReturnsAsync(Result<ExerciseDto>.Success(created));

        var result = await _sut.CreateExercise(request, null, null);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(_sut.GetExercise));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateExercise_WithVideoAndThumbnail_SetsStreamProperties()
    {
        var request = new CreateExerciseRequest
        {
            Name = "Exercise With Media",
            BodyRegionCategoryId = Guid.NewGuid()
        };

        var videoMock = new Mock<IFormFile>();
        var videoStream = new MemoryStream();
        videoMock.Setup(f => f.OpenReadStream()).Returns(videoStream);
        videoMock.Setup(f => f.FileName).Returns("exercise.mp4");
        videoMock.Setup(f => f.ContentType).Returns("video/mp4");

        var thumbnailMock = new Mock<IFormFile>();
        var thumbStream = new MemoryStream();
        thumbnailMock.Setup(f => f.OpenReadStream()).Returns(thumbStream);
        thumbnailMock.Setup(f => f.FileName).Returns("thumb.jpg");
        thumbnailMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var created = new ExerciseDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            VideoUrl = "https://cdn.example.com/exercise.mp4",
            ThumbnailUrl = "https://cdn.example.com/thumb.jpg"
        };

        _exerciseServiceMock
            .Setup(x => x.CreateExerciseAsync(
                It.Is<CreateExerciseRequest>(r =>
                    r.VideoStream != null &&
                    r.VideoFileName == "exercise.mp4" &&
                    r.ThumbnailStream != null &&
                    r.ThumbnailFileName == "thumb.jpg"),
                null))
            .ReturnsAsync(Result<ExerciseDto>.Success(created));

        var result = await _sut.CreateExercise(request, videoMock.Object, thumbnailMock.Object);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateExercise_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateExerciseRequest
        {
            Name = "New Exercise",
            BodyRegionCategoryId = Guid.NewGuid()
        };

        _exerciseServiceMock
            .Setup(x => x.CreateExerciseAsync(It.IsAny<CreateExerciseRequest>(), null))
            .ReturnsAsync(Result<ExerciseDto>.Failure("Validation error"));

        var result = await _sut.CreateExercise(request, null, null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateExercise

    [Fact]
    public async Task UpdateExercise_WhenValid_ReturnsOk()
    {
        var exerciseId = Guid.NewGuid();
        var request = new UpdateExerciseRequest
        {
            Name = "Updated Exercise",
            BodyRegionCategoryId = Guid.NewGuid(),
            Repeats = 15
        };

        var updated = new ExerciseDto
        {
            Id = exerciseId,
            Name = request.Name,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            Repeats = request.Repeats
        };

        _exerciseServiceMock
            .Setup(x => x.UpdateExerciseAsync(exerciseId, It.Is<UpdateExerciseRequest>(r => r.Name == request.Name), Guid.Empty))
            .ReturnsAsync(Result<ExerciseDto>.Success(updated));

        var result = await _sut.UpdateExercise(exerciseId, request, null, null);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateExercise_WhenNotFound_ReturnsNotFound()
    {
        var exerciseId = Guid.NewGuid();
        var request = new UpdateExerciseRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = Guid.NewGuid()
        };

        _exerciseServiceMock
            .Setup(x => x.UpdateExerciseAsync(exerciseId, It.IsAny<UpdateExerciseRequest>(), Guid.Empty))
            .ReturnsAsync(Result<ExerciseDto>.Failure("Exercise not found"));

        var result = await _sut.UpdateExercise(exerciseId, request, null, null);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateExercise_WhenServiceFails_ReturnsBadRequest()
    {
        var exerciseId = Guid.NewGuid();
        var request = new UpdateExerciseRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = Guid.NewGuid()
        };

        _exerciseServiceMock
            .Setup(x => x.UpdateExerciseAsync(exerciseId, It.IsAny<UpdateExerciseRequest>(), Guid.Empty))
            .ReturnsAsync(Result<ExerciseDto>.Failure("Validation error"));

        var result = await _sut.UpdateExercise(exerciseId, request, null, null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteExercise

    [Fact]
    public async Task DeleteExercise_WhenFound_ReturnsNoContent()
    {
        var exerciseId = Guid.NewGuid();

        _exerciseServiceMock
            .Setup(x => x.DeleteExerciseAsync(exerciseId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteExercise(exerciseId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteExercise_WhenNotFound_ReturnsNotFound()
    {
        var exerciseId = Guid.NewGuid();

        _exerciseServiceMock
            .Setup(x => x.DeleteExerciseAsync(exerciseId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Exercise not found"));

        var result = await _sut.DeleteExercise(exerciseId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteExercise_WhenServiceFails_ReturnsBadRequest()
    {
        var exerciseId = Guid.NewGuid();

        _exerciseServiceMock
            .Setup(x => x.DeleteExerciseAsync(exerciseId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Cannot delete exercise in use"));

        var result = await _sut.DeleteExercise(exerciseId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
