using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Library;

public class ClinicLibraryService : IClinicLibraryService
{
    private readonly IClinicLibraryQueryService _queryService;
    private readonly IClinicLibraryOverrideService _overrideService;
    private readonly ILogger<ClinicLibraryService> _logger;

    public ClinicLibraryService(
        IClinicLibraryQueryService queryService,
        IClinicLibraryOverrideService overrideService,
        ILogger<ClinicLibraryService> logger)
    {
        _queryService = queryService;
        _overrideService = overrideService;
        _logger = logger;
    }

    public Task<Result<LibraryItemListResponse<TreatmentDto>>> GetClinicTreatmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
        => _queryService.GetClinicTreatmentsAsync(clinicId, bodyRegionId, search, page, pageSize);

    public Task<Result<LibraryItemListResponse<ExerciseDto>>> GetClinicExercisesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
        => _queryService.GetClinicExercisesAsync(clinicId, bodyRegionId, search, page, pageSize);

    public Task<Result<LibraryItemListResponse<ModalityDto>>> GetClinicModalitiesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
        => _queryService.GetClinicModalitiesAsync(clinicId, bodyRegionId, search, page, pageSize);

    public Task<Result<LibraryItemListResponse<AssessmentDto>>> GetClinicAssessmentsAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
        => _queryService.GetClinicAssessmentsAsync(clinicId, bodyRegionId, search, page, pageSize);

    public Task<Result<LibraryItemListResponse<DeviceDto>>> GetClinicDevicesAsync(Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
        => _queryService.GetClinicDevicesAsync(clinicId, bodyRegionId, search, page, pageSize);

    public Task<Result<ClinicLibraryOverrideDto>> CreateOverrideAsync(Guid clinicId, CreateClinicLibraryOverrideRequest request)
        => _overrideService.CreateOverrideAsync(clinicId, request);

    public Task<Result<ClinicLibraryOverrideDto>> UpdateOverrideAsync(Guid clinicId, Guid overrideId, UpdateClinicLibraryOverrideRequest request)
        => _overrideService.UpdateOverrideAsync(clinicId, overrideId, request);

    public Task<Result> RemoveOverrideAsync(Guid clinicId, Guid overrideId)
        => _overrideService.RemoveOverrideAsync(clinicId, overrideId);

    public Task<Result<List<ClinicLibraryOverrideDto>>> GetClinicOverridesAsync(Guid clinicId, LibraryType? type)
        => _overrideService.GetClinicOverridesAsync(clinicId, type);

    public Task<Result<ClinicLibraryOverrideDto>> GetOverrideByIdAsync(Guid clinicId, Guid overrideId)
        => _overrideService.GetOverrideByIdAsync(clinicId, overrideId);
}
