using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Clinic;

[ApiController]
[Route("api/clinic/library")]
[Authorize]
[Produces("application/json")]
[Tags("Clinic - Library")]
public class ClinicLibraryController : BaseController
{
    private readonly IClinicLibraryService _libraryService;

    public ClinicLibraryController(IClinicLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    /// <summary>
    /// Get exercises available to this clinic (global + clinic-specific, overrides applied).
    /// </summary>
    [HttpGet("exercises")]
    [ProducesResponseType(typeof(LibraryItemListResponse<ExerciseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryItemListResponse<ExerciseDto>>> GetExercises(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result = await _libraryService.GetClinicExercisesAsync(clinicId, bodyRegionId, search, page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Get modalities available to this clinic.
    /// </summary>
    [HttpGet("modalities")]
    [ProducesResponseType(typeof(LibraryItemListResponse<ModalityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryItemListResponse<ModalityDto>>> GetModalities(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result = await _libraryService.GetClinicModalitiesAsync(clinicId, bodyRegionId, search, page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Get devices available to this clinic.
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(LibraryItemListResponse<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryItemListResponse<DeviceDto>>> GetDevices(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result = await _libraryService.GetClinicDevicesAsync(clinicId, bodyRegionId, search, page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Get assessments available to this clinic.
    /// </summary>
    [HttpGet("assessments")]
    [ProducesResponseType(typeof(LibraryItemListResponse<AssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryItemListResponse<AssessmentDto>>> GetAssessments(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result = await _libraryService.GetClinicAssessmentsAsync(clinicId, bodyRegionId, search, page, pageSize);
        return FromResult(result);
    }

    /// <summary>
    /// Get treatments available to this clinic.
    /// </summary>
    [HttpGet("treatments")]
    [ProducesResponseType(typeof(LibraryItemListResponse<TreatmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryItemListResponse<TreatmentDto>>> GetTreatments(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var clinicId = TenantId ?? Guid.Empty;
        var result = await _libraryService.GetClinicTreatmentsAsync(clinicId, bodyRegionId, search, page, pageSize);
        return FromResult(result);
    }
}
