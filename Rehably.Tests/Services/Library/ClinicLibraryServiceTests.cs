using FluentAssertions;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Services.Library;

/// <summary>
/// Safety-net tests for IClinicLibraryService contract (T067).
/// Tests verify the interface contracts that implementations must satisfy.
/// </summary>
public class ClinicLibraryServiceTests
{
    private readonly Mock<IClinicLibraryService> _serviceMock;

    public ClinicLibraryServiceTests()
    {
        _serviceMock = new Mock<IClinicLibraryService>();
    }

    // ── GetClinicTreatmentsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetClinicTreatmentsAsync_ValidClinicId_ReturnsSuccessResult()
    {
        var clinicId = Guid.NewGuid();
        var expectedItems = new List<TreatmentDto>
        {
            new() { Name = "Shoulder Exercise", Code = "T001" },
            new() { Name = "Knee Therapy", Code = "T002" }
        };
        var expectedResponse = new LibraryItemListResponse<TreatmentDto>(expectedItems, 1, 20, 2);

        _serviceMock
            .Setup(s => s.GetClinicTreatmentsAsync(clinicId, null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Success(expectedResponse));

        var result = await _serviceMock.Object.GetClinicTreatmentsAsync(clinicId, null, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetClinicTreatmentsAsync_WithBodyRegionFilter_PassesFilterToService()
    {
        var clinicId = Guid.NewGuid();
        var bodyRegionId = Guid.NewGuid();
        var filteredItems = new List<TreatmentDto>
        {
            new() { Name = "Shoulder Only Treatment", Code = "T-SHOULDER" }
        };
        var expectedResponse = new LibraryItemListResponse<TreatmentDto>(filteredItems, 1, 20, 1);

        _serviceMock
            .Setup(s => s.GetClinicTreatmentsAsync(clinicId, bodyRegionId, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Success(expectedResponse));

        var result = await _serviceMock.Object.GetClinicTreatmentsAsync(clinicId, bodyRegionId, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        _serviceMock.Verify(s => s.GetClinicTreatmentsAsync(clinicId, bodyRegionId, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetClinicTreatmentsAsync_ServiceFailure_ReturnsFailureResult()
    {
        var clinicId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetClinicTreatmentsAsync(clinicId, null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<TreatmentDto>>.Failure("Failed to get clinic treatments"));

        var result = await _serviceMock.Object.GetClinicTreatmentsAsync(clinicId, null, null, 1, 20);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Failed to get clinic treatments");
    }

    // ── GetClinicExercisesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetClinicExercisesAsync_ValidClinicId_ReturnsSuccessResult()
    {
        var clinicId = Guid.NewGuid();
        var expectedItems = new List<ExerciseDto>
        {
            new() { Name = "Bicep Curl" },
            new() { Name = "Leg Press" }
        };
        var expectedResponse = new LibraryItemListResponse<ExerciseDto>(expectedItems, 1, 20, 2);

        _serviceMock
            .Setup(s => s.GetClinicExercisesAsync(clinicId, null, null, 1, 20))
            .ReturnsAsync(Result<LibraryItemListResponse<ExerciseDto>>.Success(expectedResponse));

        var result = await _serviceMock.Object.GetClinicExercisesAsync(clinicId, null, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    // ── CreateOverrideAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateOverrideAsync_ValidRequest_ReturnsCreatedOverride()
    {
        var clinicId = Guid.NewGuid();
        var globalItemId = Guid.NewGuid();
        var request = new CreateClinicLibraryOverrideRequest
        {
            LibraryType = LibraryType.Treatment,
            GlobalItemId = globalItemId,
            IsHidden = true
        };
        var expectedOverride = new ClinicLibraryOverrideDto
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            GlobalItemId = globalItemId,
            LibraryType = LibraryType.Treatment,
            IsHidden = true,
            CreatedAt = DateTime.UtcNow
        };

        _serviceMock
            .Setup(s => s.CreateOverrideAsync(clinicId, request))
            .ReturnsAsync(Result<ClinicLibraryOverrideDto>.Success(expectedOverride));

        var result = await _serviceMock.Object.CreateOverrideAsync(clinicId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClinicId.Should().Be(clinicId);
        result.Value.GlobalItemId.Should().Be(globalItemId);
        result.Value.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOverrideAsync_GlobalItemNotFound_ReturnsFailure()
    {
        var clinicId = Guid.NewGuid();
        var request = new CreateClinicLibraryOverrideRequest
        {
            LibraryType = LibraryType.Treatment,
            GlobalItemId = Guid.NewGuid(),
            IsHidden = true
        };

        _serviceMock
            .Setup(s => s.CreateOverrideAsync(clinicId, request))
            .ReturnsAsync(Result<ClinicLibraryOverrideDto>.Failure("Global Treatment item with ID not found"));

        var result = await _serviceMock.Object.CreateOverrideAsync(clinicId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    // ── GetOverrideByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOverrideByIdAsync_ExistingOverride_ReturnsOverride()
    {
        var clinicId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var expectedOverride = new ClinicLibraryOverrideDto
        {
            Id = overrideId,
            ClinicId = clinicId,
            LibraryType = LibraryType.Exercise,
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        };

        _serviceMock
            .Setup(s => s.GetOverrideByIdAsync(clinicId, overrideId))
            .ReturnsAsync(Result<ClinicLibraryOverrideDto>.Success(expectedOverride));

        var result = await _serviceMock.Object.GetOverrideByIdAsync(clinicId, overrideId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(overrideId);
        result.Value.ClinicId.Should().Be(clinicId);
    }

    [Fact]
    public async Task GetOverrideByIdAsync_NonExistentOverride_ReturnsFailure()
    {
        var clinicId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetOverrideByIdAsync(clinicId, nonExistentId))
            .ReturnsAsync(Result<ClinicLibraryOverrideDto>.Failure("Override not found"));

        var result = await _serviceMock.Object.GetOverrideByIdAsync(clinicId, nonExistentId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Override not found");
    }

    // ── GetClinicOverridesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetClinicOverridesAsync_FilteredByType_ReturnsFilteredOverrides()
    {
        var clinicId = Guid.NewGuid();
        var overrides = new List<ClinicLibraryOverrideDto>
        {
            new ClinicLibraryOverrideDto { LibraryType = LibraryType.Treatment, ClinicId = clinicId, CreatedAt = DateTime.UtcNow },
            new ClinicLibraryOverrideDto { LibraryType = LibraryType.Treatment, ClinicId = clinicId, CreatedAt = DateTime.UtcNow }
        };

        _serviceMock
            .Setup(s => s.GetClinicOverridesAsync(clinicId, LibraryType.Treatment))
            .ReturnsAsync(Result<List<ClinicLibraryOverrideDto>>.Success(overrides));

        var result = await _serviceMock.Object.GetClinicOverridesAsync(clinicId, LibraryType.Treatment);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.All(o => o.LibraryType == LibraryType.Treatment).Should().BeTrue();
    }

    [Fact]
    public async Task GetClinicOverridesAsync_NoTypeFilter_ReturnsAllOverrides()
    {
        var clinicId = Guid.NewGuid();
        var overrides = new List<ClinicLibraryOverrideDto>
        {
            new ClinicLibraryOverrideDto { LibraryType = LibraryType.Treatment, CreatedAt = DateTime.UtcNow },
            new ClinicLibraryOverrideDto { LibraryType = LibraryType.Exercise, CreatedAt = DateTime.UtcNow }
        };

        _serviceMock
            .Setup(s => s.GetClinicOverridesAsync(clinicId, null))
            .ReturnsAsync(Result<List<ClinicLibraryOverrideDto>>.Success(overrides));

        var result = await _serviceMock.Object.GetClinicOverridesAsync(clinicId, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
