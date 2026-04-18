using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AdminAssessmentsControllerTests
{
    private readonly Mock<IAssessmentService> _assessmentServiceMock;
    private readonly AdminAssessmentsController _sut;

    public AdminAssessmentsControllerTests()
    {
        _assessmentServiceMock = new Mock<IAssessmentService>();
        _sut = new AdminAssessmentsController(_assessmentServiceMock.Object);
    }

    #region GetAssessments

    [Fact]
    public async Task GetAssessments_WhenServiceSucceeds_ReturnsOkWithPaginatedList()
    {
        var response = new LibraryItemListResponse<AssessmentDto>
        {
            Items = new List<AssessmentDto>
            {
                new() { Id = Guid.NewGuid(), Name = "ROM Assessment", Code = "ASS001" },
                new() { Id = Guid.NewGuid(), Name = "Pain Scale", Code = "ASS002" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _assessmentServiceMock
            .Setup(x => x.GetAssessmentsAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<AssessmentDto>>.Success(response));

        var result = await _sut.GetAssessments();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetAssessments_WithBodyRegionFilter_PassesFilterToService()
    {
        var bodyRegionId = Guid.NewGuid();
        var response = new LibraryItemListResponse<AssessmentDto>
        {
            Items = new List<AssessmentDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _assessmentServiceMock
            .Setup(x => x.GetAssessmentsAsync(bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<AssessmentDto>>.Success(response));

        var result = await _sut.GetAssessments(bodyRegionId);

        result.Result.Should().BeOfType<OkObjectResult>();
        _assessmentServiceMock.Verify(x => x.GetAssessmentsAsync(bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAssessments_WhenServiceFails_ReturnsBadRequest()
    {
        _assessmentServiceMock
            .Setup(x => x.GetAssessmentsAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<AssessmentDto>>.Failure("Failed to retrieve assessments"));

        var result = await _sut.GetAssessments();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetAssessment

    [Fact]
    public async Task GetAssessment_WhenFound_ReturnsOk()
    {
        var assessmentId = Guid.NewGuid();
        var assessment = new AssessmentDto
        {
            Id = assessmentId,
            Name = "ROM Assessment",
            Code = "ASS001",
            TimePoint = AssessmentTimePoint.Baseline,
            AccessTier = LibraryAccessTier.Free
        };

        _assessmentServiceMock
            .Setup(x => x.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Result<AssessmentDto>.Success(assessment));

        var result = await _sut.GetAssessment(assessmentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(assessment);
    }

    [Fact]
    public async Task GetAssessment_WhenNotFound_ReturnsNotFound()
    {
        var assessmentId = Guid.NewGuid();

        _assessmentServiceMock
            .Setup(x => x.GetAssessmentByIdAsync(assessmentId))
            .ReturnsAsync(Result<AssessmentDto>.Failure("Assessment not found"));

        var result = await _sut.GetAssessment(assessmentId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateAssessment

    [Fact]
    public async Task CreateAssessment_WhenValid_ReturnsCreatedAtAction()
    {
        var request = new CreateAssessmentRequest
        {
            Name = "New Assessment",
            Code = "ASS003",
            TimePoint = AssessmentTimePoint.Biweekly,
            Description = "Test description"
        };

        var created = new AssessmentDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            TimePoint = request.TimePoint,
            Description = request.Description,
            ClinicId = null
        };

        _assessmentServiceMock
            .Setup(x => x.CreateAssessmentAsync(request, null))
            .ReturnsAsync(Result<AssessmentDto>.Success(created));

        var result = await _sut.CreateAssessment(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(_sut.GetAssessment));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateAssessment_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateAssessmentRequest
        {
            Name = "New Assessment",
            Code = "ASS003",
            TimePoint = AssessmentTimePoint.Baseline
        };

        _assessmentServiceMock
            .Setup(x => x.CreateAssessmentAsync(request, null))
            .ReturnsAsync(Result<AssessmentDto>.Failure("Duplicate code"));

        var result = await _sut.CreateAssessment(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateAssessment

    [Fact]
    public async Task UpdateAssessment_WhenValid_ReturnsOk()
    {
        var assessmentId = Guid.NewGuid();
        var request = new UpdateAssessmentRequest
        {
            Name = "Updated Assessment",
            Description = "Updated description"
        };

        var updated = new AssessmentDto
        {
            Id = assessmentId,
            Name = request.Name,
            Description = request.Description
        };

        _assessmentServiceMock
            .Setup(x => x.UpdateAssessmentAsync(assessmentId, request, Guid.Empty))
            .ReturnsAsync(Result<AssessmentDto>.Success(updated));

        var result = await _sut.UpdateAssessment(assessmentId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateAssessment_WhenNotFound_ReturnsNotFound()
    {
        var assessmentId = Guid.NewGuid();
        var request = new UpdateAssessmentRequest { Name = "Updated" };

        _assessmentServiceMock
            .Setup(x => x.UpdateAssessmentAsync(assessmentId, request, Guid.Empty))
            .ReturnsAsync(Result<AssessmentDto>.Failure("Assessment not found"));

        var result = await _sut.UpdateAssessment(assessmentId, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateAssessment_WhenServiceFails_ReturnsBadRequest()
    {
        var assessmentId = Guid.NewGuid();
        var request = new UpdateAssessmentRequest { Name = "Updated" };

        _assessmentServiceMock
            .Setup(x => x.UpdateAssessmentAsync(assessmentId, request, Guid.Empty))
            .ReturnsAsync(Result<AssessmentDto>.Failure("Validation error"));

        var result = await _sut.UpdateAssessment(assessmentId, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteAssessment

    [Fact]
    public async Task DeleteAssessment_WhenFound_ReturnsNoContent()
    {
        var assessmentId = Guid.NewGuid();

        _assessmentServiceMock
            .Setup(x => x.DeleteAssessmentAsync(assessmentId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteAssessment(assessmentId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAssessment_WhenNotFound_ReturnsNotFound()
    {
        var assessmentId = Guid.NewGuid();

        _assessmentServiceMock
            .Setup(x => x.DeleteAssessmentAsync(assessmentId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Assessment not found"));

        var result = await _sut.DeleteAssessment(assessmentId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteAssessment_WhenServiceFails_ReturnsBadRequest()
    {
        var assessmentId = Guid.NewGuid();

        _assessmentServiceMock
            .Setup(x => x.DeleteAssessmentAsync(assessmentId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Cannot delete assessment in use"));

        var result = await _sut.DeleteAssessment(assessmentId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
