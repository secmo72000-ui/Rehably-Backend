using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class ModalityService : IModalityService
{
    private readonly IModalityRepository _modalityRepository;
    private readonly ILogger<ModalityService> _logger;

    public ModalityService(IModalityRepository modalityRepository, ILogger<ModalityService> logger)
    {
        _modalityRepository = modalityRepository;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<ModalityDto>>> GetModalitiesAsync(Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _modalityRepository.Query().Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();

        var modalities = (await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Adapt<List<ModalityDto>>();

        var response = LibraryItemListResponse<ModalityDto>.Create(modalities, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<ModalityDto>>.Success(response);
    }

    public async Task<Result<ModalityDto>> GetModalityByIdAsync(Guid id)
    {
        var modality = await _modalityRepository.GetWithDetailsAsync(id);
        if (modality == null)
            return Result<ModalityDto>.Failure("Modality not found");

        return Result<ModalityDto>.Success(modality.Adapt<ModalityDto>());
    }

    public async Task<Result<ModalityDto>> CreateModalityAsync(CreateModalityRequest request, Guid? clinicId)
    {
        var modality = new Modality
        {
            ClinicId = clinicId,
            Code = request.Code,
            Name = request.Name,
            NameArabic = request.NameArabic,
            ModalityType = request.ModalityType,
            TherapeuticCategory = request.TherapeuticCategory,
            MainGoal = request.MainGoal,
            ParametersNotes = request.ParametersNotes,
            ClinicalNotes = request.ClinicalNotes,
            MinDurationWeeks = request.MinDurationWeeks,
            MaxDurationWeeks = request.MaxDurationWeeks,
            MinSessions = request.MinSessions,
            MaxSessions = request.MaxSessions,
            RelatedConditionCodes = request.RelatedConditionCodes,
            AccessTier = request.AccessTier,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _modalityRepository.AddAsync(modality);
        await _modalityRepository.SaveChangesAsync();

        _logger.LogInformation("Created modality {ModalityId} ({ModalityName}) for clinic {ClinicId}",
            modality.Id, modality.Name, clinicId ?? Guid.Empty);

        return Result<ModalityDto>.Success(modality.Adapt<ModalityDto>());
    }

    public async Task<Result<ModalityDto>> UpdateModalityAsync(Guid id, UpdateModalityRequest request, Guid clinicId)
    {
        var modality = await _modalityRepository.GetWithDetailsAsync(id);
        if (modality == null)
            return Result<ModalityDto>.Failure("Modality not found");

        if (clinicId != Guid.Empty && modality.ClinicId != clinicId)
            return Result<ModalityDto>.Failure("You can only update modalities owned by your clinic");

        modality.Name = request.Name;
        modality.NameArabic = request.NameArabic;
        modality.ModalityType = request.ModalityType;
        modality.TherapeuticCategory = request.TherapeuticCategory;
        modality.MainGoal = request.MainGoal;
        modality.ParametersNotes = request.ParametersNotes;
        modality.ClinicalNotes = request.ClinicalNotes;
        modality.MinDurationWeeks = request.MinDurationWeeks;
        modality.MaxDurationWeeks = request.MaxDurationWeeks;
        modality.MinSessions = request.MinSessions;
        modality.MaxSessions = request.MaxSessions;
        modality.RelatedConditionCodes = request.RelatedConditionCodes;
        modality.AccessTier = request.AccessTier;
        modality.UpdatedAt = DateTime.UtcNow;

        await _modalityRepository.UpdateAsync(modality);
        await _modalityRepository.SaveChangesAsync();

        _logger.LogInformation("Updated modality {ModalityId} ({ModalityName})", modality.Id, modality.Name);

        return Result<ModalityDto>.Success(modality.Adapt<ModalityDto>());
    }

    public async Task<Result> DeleteModalityAsync(Guid id, Guid clinicId)
    {
        var modality = await _modalityRepository.GetByIdAsync(id);
        if (modality == null)
            return Result.Failure("Modality not found");

        if (clinicId != Guid.Empty && modality.ClinicId != clinicId)
            return Result.Failure("You can only delete modalities owned by your clinic");

        modality.IsDeleted = true;
        modality.UpdatedAt = DateTime.UtcNow;

        await _modalityRepository.UpdateAsync(modality);
        await _modalityRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted modality {ModalityId} ({ModalityName})", modality.Id, modality.Name);

        return Result.Success();
    }

}
