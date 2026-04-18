using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class TreatmentStageService : ITreatmentStageService
{
    private readonly ITreatmentStageRepository _stageRepository;
    private readonly IBodyRegionCategoryRepository _bodyRegionRepository;
    private readonly ILogger<TreatmentStageService> _logger;

    public TreatmentStageService(
        ITreatmentStageRepository stageRepository,
        IBodyRegionCategoryRepository bodyRegionRepository,
        ILogger<TreatmentStageService> logger)
    {
        _stageRepository = stageRepository;
        _bodyRegionRepository = bodyRegionRepository;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<TreatmentStageDto>>> GetStagesAsync(
        Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _stageRepository.Query()
            .Where(s => !s.IsDeleted);

        if (bodyRegionId.HasValue)
            query = query.Where(s => s.BodyRegionId == bodyRegionId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.ToLower().Contains(search.ToLower()));

        var totalCount = query.Count();

        var stages = query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .Adapt<List<TreatmentStageDto>>();

        var response = LibraryItemListResponse<TreatmentStageDto>.Create(stages, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<TreatmentStageDto>>.Success(response);
    }

    public async Task<Result<LibraryItemListResponse<TreatmentStageDto>>> GetClinicStagesAsync(
        Guid clinicId, Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _stageRepository.Query()
            .Where(s => s.TenantId == clinicId && !s.IsDeleted);

        if (bodyRegionId.HasValue)
            query = query.Where(s => s.BodyRegionId == bodyRegionId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.ToLower().Contains(search.ToLower()));

        var totalCount = query.Count();

        var stages = query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .Adapt<List<TreatmentStageDto>>();

        var response = LibraryItemListResponse<TreatmentStageDto>.Create(stages, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<TreatmentStageDto>>.Success(response);
    }

    public async Task<Result<TreatmentStageDto>> GetStageByIdAsync(Guid id)
    {
        var stage = await _stageRepository.GetWithDetailsAsync(id);

        if (stage == null)
            return Result<TreatmentStageDto>.Failure("Treatment stage not found");

        return Result<TreatmentStageDto>.Success(stage.Adapt<TreatmentStageDto>());
    }

    public async Task<Result<TreatmentStageDto>> CreateStageAsync(CreateTreatmentStageRequest request, Guid clinicId)
    {
        if (request.BodyRegionId.HasValue)
        {
            var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionId.Value && b.IsActive);
            if (!bodyRegionExists)
                return Result<TreatmentStageDto>.Failure("Body region not found");
        }

        var stage = new TreatmentStage
        {
            TenantId = clinicId,
            BodyRegionId = request.BodyRegionId,
            Code = request.Code,
            Name = request.Name,
            NameArabic = request.NameArabic,
            Description = request.Description,
            MinWeeks = request.MinWeeks,
            MaxWeeks = request.MaxWeeks,
            MinSessions = request.MinSessions,
            MaxSessions = request.MaxSessions,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _stageRepository.AddAsync(stage);
        await _stageRepository.SaveChangesAsync();

        _logger.LogInformation("Created treatment stage {StageId} ({StageName}) for clinic {ClinicId}",
            stage.Id, stage.Name, clinicId);

        var saved = await _stageRepository.Query()
            .AsNoTracking()
            .Include(s => s.BodyRegion)
            .FirstOrDefaultAsync(s => s.Id == stage.Id);
        return Result<TreatmentStageDto>.Success(saved!.Adapt<TreatmentStageDto>());
    }

    public async Task<Result<TreatmentStageDto>> UpdateStageAsync(Guid id, UpdateTreatmentStageRequest request, Guid clinicId)
    {
        var stage = await _stageRepository.GetWithDetailsAsync(id);
        if (stage == null)
            return Result<TreatmentStageDto>.Failure("Treatment stage not found");

        if (stage.TenantId != clinicId)
            return Result<TreatmentStageDto>.Failure("You can only update treatment stages owned by your clinic");

        if (request.BodyRegionId.HasValue && request.BodyRegionId != stage.BodyRegionId)
        {
            var bodyRegionExists = await _bodyRegionRepository.AnyAsync(b => b.Id == request.BodyRegionId.Value && b.IsActive);
            if (!bodyRegionExists)
                return Result<TreatmentStageDto>.Failure("Body region not found");
        }

        stage.BodyRegionId = request.BodyRegionId;
        stage.Name = request.Name;
        stage.NameArabic = request.NameArabic;
        stage.Description = request.Description;
        stage.MinWeeks = request.MinWeeks;
        stage.MaxWeeks = request.MaxWeeks;
        stage.MinSessions = request.MinSessions;
        stage.MaxSessions = request.MaxSessions;
        stage.UpdatedAt = DateTime.UtcNow;

        await _stageRepository.UpdateAsync(stage);
        await _stageRepository.SaveChangesAsync();

        _logger.LogInformation("Updated treatment stage {StageId} ({StageName})", stage.Id, stage.Name);

        var saved = await _stageRepository.Query()
            .AsNoTracking()
            .Include(s => s.BodyRegion)
            .FirstOrDefaultAsync(s => s.Id == stage.Id);
        return Result<TreatmentStageDto>.Success(saved!.Adapt<TreatmentStageDto>());
    }

    public async Task<Result> DeleteStageAsync(Guid id, Guid clinicId)
    {
        var stage = await _stageRepository.GetByIdAsync(id);
        if (stage == null)
            return Result.Failure("Treatment stage not found");

        if (clinicId != Guid.Empty && stage.TenantId != clinicId)
            return Result.Failure("You can only delete treatment stages owned by your clinic");

        stage.IsDeleted = true;
        stage.DeletedAt = DateTime.UtcNow;
        stage.UpdatedAt = DateTime.UtcNow;

        await _stageRepository.UpdateAsync(stage);
        await _stageRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted treatment stage {StageId} ({StageName})", stage.Id, stage.Name);

        return Result.Success();
    }
}
