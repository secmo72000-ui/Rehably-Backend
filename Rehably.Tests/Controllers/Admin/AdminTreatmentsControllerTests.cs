using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class AdminTreatmentsControllerTests
{
    private readonly Mock<ITreatmentService> _treatmentServiceMock;
    private readonly AdminTreatmentsController _sut;

    public AdminTreatmentsControllerTests()
    {
        _treatmentServiceMock = new Mock<ITreatmentService>();
        _sut = new AdminTreatmentsController(_treatmentServiceMock.Object);
    }

    #region GetTreatments

    [Fact]
    public async Task GetTreatments_WhenServiceSucceeds_ReturnsOkWithPaginatedList()
    {
        var response = new LibraryItemListResponse<TreatmentDto>
        {
            Items = new List<TreatmentDto>
            {
                new() { Id = Guid.NewGuid(), Name = "ACL Rehab", Code = "TRT001", BodyRegionCategoryId = Guid.NewGuid() },
                new() { Id = Guid.NewGuid(), Name = "Rotator Cuff", Code = "TRT002", BodyRegionCategoryId = Guid.NewGuid() }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _treatmentServiceMock
            .Setup(x => x.GetTreatmentsAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Success(response));

        var result = await _sut.GetTreatments();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetTreatments_WithBodyRegionFilter_PassesFilterToService()
    {
        var bodyRegionId = Guid.NewGuid();
        var response = new LibraryItemListResponse<TreatmentDto>
        {
            Items = new List<TreatmentDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _treatmentServiceMock
            .Setup(x => x.GetTreatmentsAsync(bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Success(response));

        var result = await _sut.GetTreatments(bodyRegionId);

        result.Result.Should().BeOfType<OkObjectResult>();
        _treatmentServiceMock.Verify(x => x.GetTreatmentsAsync(bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetTreatments_WhenServiceFails_ReturnsBadRequest()
    {
        _treatmentServiceMock
            .Setup(x => x.GetTreatmentsAsync(null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Failure("Failed to retrieve treatments"));

        var result = await _sut.GetTreatments();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetTreatment

    [Fact]
    public async Task GetTreatment_WhenFound_ReturnsOk()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = new TreatmentDto
        {
            Id = treatmentId,
            Name = "ACL Rehab",
            Code = "TRT001",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Knee",
            MinDurationWeeks = 8,
            MaxDurationWeeks = 16,
            ExpectedSessions = 24,
            AccessTier = LibraryAccessTier.Free
        };

        _treatmentServiceMock
            .Setup(x => x.GetTreatmentByIdAsync(treatmentId))
            .ReturnsAsync(Result<TreatmentDto>.Success(treatment));

        var result = await _sut.GetTreatment(treatmentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(treatment);
    }

    [Fact]
    public async Task GetTreatment_WhenNotFound_ReturnsNotFound()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentServiceMock
            .Setup(x => x.GetTreatmentByIdAsync(treatmentId))
            .ReturnsAsync(Result<TreatmentDto>.Failure("Treatment not found"));

        var result = await _sut.GetTreatment(treatmentId);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateTreatment

    [Fact]
    public async Task CreateTreatment_WhenValid_ReturnsCreatedAtAction()
    {
        var request = new CreateTreatmentRequest
        {
            Name = "New Treatment",
            Code = "TRT003",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Shoulder",
            MinDurationWeeks = 6,
            MaxDurationWeeks = 12,
            ExpectedSessions = 18
        };

        var created = new TreatmentDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            AffectedArea = request.AffectedArea,
            MinDurationWeeks = request.MinDurationWeeks,
            MaxDurationWeeks = request.MaxDurationWeeks,
            ExpectedSessions = request.ExpectedSessions,
            ClinicId = null
        };

        _treatmentServiceMock
            .Setup(x => x.CreateTreatmentAsync(request, null))
            .ReturnsAsync(Result<TreatmentDto>.Success(created));

        var result = await _sut.CreateTreatment(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(_sut.GetTreatment));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateTreatment_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateTreatmentRequest
        {
            Name = "New Treatment",
            Code = "TRT003",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Shoulder",
            MinDurationWeeks = 6,
            MaxDurationWeeks = 12,
            ExpectedSessions = 18
        };

        _treatmentServiceMock
            .Setup(x => x.CreateTreatmentAsync(request, null))
            .ReturnsAsync(Result<TreatmentDto>.Failure("Duplicate code"));

        var result = await _sut.CreateTreatment(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateTreatment

    [Fact]
    public async Task UpdateTreatment_WhenValid_ReturnsOk()
    {
        var treatmentId = Guid.NewGuid();
        var request = new UpdateTreatmentRequest
        {
            Name = "Updated Treatment",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Updated Area",
            MinDurationWeeks = 4,
            MaxDurationWeeks = 8,
            ExpectedSessions = 12
        };

        var updated = new TreatmentDto
        {
            Id = treatmentId,
            Name = request.Name,
            BodyRegionCategoryId = request.BodyRegionCategoryId,
            AffectedArea = request.AffectedArea,
            MinDurationWeeks = request.MinDurationWeeks,
            MaxDurationWeeks = request.MaxDurationWeeks,
            ExpectedSessions = request.ExpectedSessions
        };

        _treatmentServiceMock
            .Setup(x => x.UpdateTreatmentAsync(treatmentId, request, Guid.Empty))
            .ReturnsAsync(Result<TreatmentDto>.Success(updated));

        var result = await _sut.UpdateTreatment(treatmentId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateTreatment_WhenNotFound_ReturnsNotFound()
    {
        var treatmentId = Guid.NewGuid();
        var request = new UpdateTreatmentRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Area"
        };

        _treatmentServiceMock
            .Setup(x => x.UpdateTreatmentAsync(treatmentId, request, Guid.Empty))
            .ReturnsAsync(Result<TreatmentDto>.Failure("Treatment not found"));

        var result = await _sut.UpdateTreatment(treatmentId, request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTreatment_WhenServiceFails_ReturnsBadRequest()
    {
        var treatmentId = Guid.NewGuid();
        var request = new UpdateTreatmentRequest
        {
            Name = "Updated",
            BodyRegionCategoryId = Guid.NewGuid(),
            AffectedArea = "Area"
        };

        _treatmentServiceMock
            .Setup(x => x.UpdateTreatmentAsync(treatmentId, request, Guid.Empty))
            .ReturnsAsync(Result<TreatmentDto>.Failure("Validation error"));

        var result = await _sut.UpdateTreatment(treatmentId, request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteTreatment

    [Fact]
    public async Task DeleteTreatment_WhenFound_ReturnsNoContent()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentServiceMock
            .Setup(x => x.DeleteTreatmentAsync(treatmentId, Guid.Empty))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DeleteTreatment(treatmentId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTreatment_WhenNotFound_ReturnsNotFound()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentServiceMock
            .Setup(x => x.DeleteTreatmentAsync(treatmentId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Treatment not found"));

        var result = await _sut.DeleteTreatment(treatmentId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTreatment_WhenServiceFails_ReturnsBadRequest()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentServiceMock
            .Setup(x => x.DeleteTreatmentAsync(treatmentId, Guid.Empty))
            .ReturnsAsync(Result.Failure("Cannot delete treatment in use"));

        var result = await _sut.DeleteTreatment(treatmentId);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
