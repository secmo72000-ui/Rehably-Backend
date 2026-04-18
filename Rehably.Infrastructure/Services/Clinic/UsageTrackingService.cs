using Microsoft.EntityFrameworkCore;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

public class UsageTrackingService : IUsageTrackingService
{
    private const string FeatureCodeStorage = "storage";
    private const string FeatureCodePatients = "patients";
    private const string FeatureCodeUsers = "users";

    private readonly IClinicRepository _clinicRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionFeatureUsageRepository _featureUsageRepository;
    private readonly IRepository<UsageHistory> _usageHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsageService _usageService;

    public UsageTrackingService(
        IClinicRepository clinicRepository,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionFeatureUsageRepository featureUsageRepository,
        IRepository<UsageHistory> usageHistoryRepository,
        IUnitOfWork unitOfWork,
        IUsageService usageService)
    {
        _clinicRepository = clinicRepository;
        _subscriptionRepository = subscriptionRepository;
        _featureUsageRepository = featureUsageRepository;
        _usageHistoryRepository = usageHistoryRepository;
        _unitOfWork = unitOfWork;
        _usageService = usageService;
    }

    public async Task RecordStorageUsageAsync(Guid clinicId, long bytesUsed)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return;
            }

            clinic.StorageUsedBytes = bytesUsed;

            await RecordUsageHistoryAsync(clinicId, MetricType.Storage, bytesUsed);

            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (subscription != null)
            {
                var storageFeature = await _featureUsageRepository
                    .GetBySubscriptionAndFeatureCodeAsync(subscription.Id, FeatureCodeStorage);

                if (storageFeature != null)
                {
                    storageFeature.Used = (int)(bytesUsed / (1024L * 1024 * 1024));
                    storageFeature.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RecordPatientCountAsync(Guid clinicId, int count)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return;
            }

            clinic.PatientsCount = count;

            await RecordUsageHistoryAsync(clinicId, MetricType.Patients, count);

            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (subscription != null)
            {
                var patientsFeature = await _featureUsageRepository
                    .GetBySubscriptionAndFeatureCodeAsync(subscription.Id, FeatureCodePatients);

                if (patientsFeature != null)
                {
                    patientsFeature.Used = count;
                    patientsFeature.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RecordUserCountAsync(Guid clinicId, int count)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return;
            }

            clinic.UsersCount = count;

            await RecordUsageHistoryAsync(clinicId, MetricType.Users, count);

            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinicId);
            if (subscription != null)
            {
                var usersFeature = await _featureUsageRepository
                    .GetBySubscriptionAndFeatureCodeAsync(subscription.Id, FeatureCodeUsers);

                if (usersFeature != null)
                {
                    usersFeature.Used = count;
                    usersFeature.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RecordApiCallAsync(Guid clinicId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await RecordUsageHistoryAsync(clinicId, MetricType.ApiCalls, 1);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<UsageStatisticsResponse> GetUsageStatisticsAsync(Guid clinicId, int days = 30)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);

        if (clinic is not { } clinicEntity)
        {
            throw new InvalidOperationException("Clinic not found");
        }

        var fromDate = DateTime.UtcNow.AddDays(-days);
        var history = await _usageHistoryRepository.FindAsync(
            u => u.ClinicId == clinicId && u.RecordedAt >= fromDate);

        var storageUsage = await _usageService.GetUsageAsync(clinicId, FeatureCodeStorage);
        var patientsUsage = await _usageService.GetUsageAsync(clinicId, FeatureCodePatients);
        var usersUsage = await _usageService.GetUsageAsync(clinicId, FeatureCodeUsers);

        var storageLimitBytes = storageUsage.IsSuccess
            ? storageUsage.Value.Limit * 1024L * 1024 * 1024
            : clinicEntity.StorageLimitBytes;

        var patientsLimit = patientsUsage.IsSuccess && patientsUsage.Value.Limit > 0
            ? patientsUsage.Value.Limit
            : clinicEntity.PatientsLimit;

        var usersLimit = usersUsage.IsSuccess && usersUsage.Value.Limit > 0
            ? usersUsage.Value.Limit
            : clinicEntity.UsersLimit;

        var orderedHistory = history.OrderBy(u => u.RecordedAt).ToList();

        return new UsageStatisticsResponse
        {
            ClinicId = clinicEntity.Id,
            ClinicName = clinicEntity.Name,
            StorageUsedBytes = clinicEntity.StorageUsedBytes,
            StorageLimitBytes = storageLimitBytes,
            StorageUsedPercentage = storageLimitBytes > 0
                ? (decimal)clinicEntity.StorageUsedBytes / storageLimitBytes * 100
                : 0,
            StorageUsedFormatted = FormatBytes(clinicEntity.StorageUsedBytes),
            StorageLimitFormatted = FormatBytes(storageLimitBytes),
            PatientsCount = clinicEntity.PatientsCount,
            PatientsLimit = patientsLimit,
            PatientsUsedPercentage = patientsLimit.HasValue && patientsLimit.Value > 0
                ? (decimal)clinicEntity.PatientsCount / patientsLimit.Value * 100
                : 0,
            UsersCount = clinicEntity.UsersCount,
            UsersLimit = usersLimit,
            UsersUsedPercentage = usersLimit.HasValue && usersLimit.Value > 0
                ? (decimal)clinicEntity.UsersCount / usersLimit.Value * 100
                : 0,
            History = orderedHistory.Select(h => new UsageHistoryItem
            {
                Date = h.RecordedAt,
                MetricType = h.MetricType,
                Value = h.Value,
                ValueFormatted = h.MetricType == MetricType.Storage
                    ? FormatBytes(h.Value)
                    : h.Value.ToString()
            }).ToList()
        };
    }

    public async Task<bool> IsStorageLimitExceededAsync(Guid clinicId)
    {
        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null)
        {
            return true;
        }

        var canUseResult = await _usageService.CanUseFeatureAsync(clinicId, FeatureCodeStorage);
        if (!canUseResult.IsSuccess || !canUseResult.Value)
        {
            return true;
        }

        var limitResult = await _usageService.GetUsageAsync(clinicId, FeatureCodeStorage);
        if (limitResult.IsSuccess)
        {
            var limitGB = limitResult.Value.Limit;
            var limitBytes = limitGB * 1024L * 1024 * 1024;
            return clinic.StorageUsedBytes >= limitBytes;
        }

        return clinic.StorageUsedBytes >= clinic.StorageLimitBytes;
    }

    public async Task<bool> IsPatientsLimitExceededAsync(Guid clinicId)
    {
        var canUseResult = await _usageService.CanUseFeatureAsync(clinicId, FeatureCodePatients);
        if (!canUseResult.IsSuccess || !canUseResult.Value)
        {
            return true;
        }

        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null)
        {
            return true;
        }

        var limitResult = await _usageService.GetUsageAsync(clinicId, FeatureCodePatients);
        if (limitResult.IsSuccess)
        {
            return clinic.PatientsCount >= limitResult.Value.Limit;
        }

        if (!clinic.PatientsLimit.HasValue)
        {
            return false;
        }
        return clinic.PatientsCount >= clinic.PatientsLimit.Value;
    }

    public async Task<bool> IsUsersLimitExceededAsync(Guid clinicId)
    {
        var canUseResult = await _usageService.CanUseFeatureAsync(clinicId, FeatureCodeUsers);
        if (!canUseResult.IsSuccess || !canUseResult.Value)
        {
            return true;
        }

        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null)
        {
            return true;
        }

        var limitResult = await _usageService.GetUsageAsync(clinicId, FeatureCodeUsers);
        if (limitResult.IsSuccess)
        {
            return clinic.UsersCount >= limitResult.Value.Limit;
        }

        if (!clinic.UsersLimit.HasValue)
        {
            return false;
        }
        return clinic.UsersCount >= clinic.UsersLimit.Value;
    }

    public async Task<List<UsageHistory>> GetUsageHistoryAsync(Guid clinicId, MetricType metricType, DateTime fromDate, DateTime toDate)
    {
        var history = await _usageHistoryRepository.FindAsync(
            u => u.ClinicId == clinicId
               && u.MetricType == metricType
               && u.RecordedAt >= fromDate
               && u.RecordedAt <= toDate);

        return history.OrderBy(u => u.RecordedAt).ToList();
    }

    private async Task RecordUsageHistoryAsync(Guid clinicId, MetricType metricType, long value)
    {
        var history = new UsageHistory
        {
            ClinicId = clinicId,
            MetricType = metricType,
            Value = value,
            RecordedAt = DateTime.UtcNow
        };

        await _usageHistoryRepository.AddAsync(history);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
