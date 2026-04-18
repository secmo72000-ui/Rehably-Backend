using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Interfaces;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Services.Clinic;

public class ClinicUsageTrackingService : IClinicUsageTrackingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUsageRecordRepository _usageRecordRepository;
    private readonly ILogger<ClinicUsageTrackingService> _logger;
    private readonly IClock _clock;

    public ClinicUsageTrackingService(
        IUnitOfWork unitOfWork,
        IClinicRepository clinicRepository,
        IUsageRecordRepository usageRecordRepository,
        ILogger<ClinicUsageTrackingService> logger,
        IClock clock)
    {
        _unitOfWork = unitOfWork;
        _clinicRepository = clinicRepository;
        _usageRecordRepository = usageRecordRepository;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result> IncrementUsageAsync(
        Guid clinicId,
        UsageMetric metric,
        long delta = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clinic = await _clinicRepository.GetWithSubscriptionAndPackageAsync(clinicId);

            if (clinic == null)
            {
                return Result.Failure($"Clinic {clinicId} not found");
            }

            if (clinic.CurrentSubscription == null)
            {
                return Result.Failure($"No active subscription found for clinic {clinicId}");
            }

            var package = clinic.CurrentSubscription.Package;
            var currentPeriod = GetCurrentMonthPeriod();

            var usageRecord = await _usageRecordRepository.GetByClinicMetricPeriodAsync(clinicId, metric, currentPeriod);

            if (usageRecord == null)
            {
                usageRecord = new UsageRecord
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    Metric = metric,
                    Value = 0,
                    Period = currentPeriod
                };
                await _usageRecordRepository.AddAsync(usageRecord);
            }

            if (!IsWithinLimit(usageRecord.Value + delta, metric, package))
            {
                return Result.Failure($"Usage limit exceeded for {metric}");
            }

            usageRecord.Value += delta;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Incremented {Metric} by {Delta} for clinic {ClinicId}", metric, delta, clinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing usage for clinic {ClinicId}", clinicId);
            return Result.Failure($"An error occurred while tracking usage: {ex.Message}");
        }
    }

    public async Task<Result<long>> GetCurrentUsageAsync(
        Guid clinicId,
        UsageMetric metric,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPeriod = GetCurrentMonthPeriod();

            var usageRecord = await _usageRecordRepository.GetByClinicMetricPeriodAsync(clinicId, metric, currentPeriod);

            return Result<long>.Success(usageRecord?.Value ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage for clinic {ClinicId}", clinicId);
            return Result<long>.Failure($"An error occurred while getting usage: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<UsageMetric, long>>> GetAllUsageAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPeriod = GetCurrentMonthPeriod();

            var usageRecords = await _usageRecordRepository.GetByClinicAndPeriodAsync(clinicId, currentPeriod);

            var usageByMetric = new Dictionary<UsageMetric, long>();
            foreach (var metric in Enum.GetValues<UsageMetric>())
            {
                usageByMetric[metric] = usageRecords.FirstOrDefault(u => u.Metric == metric)?.Value ?? 0;
            }

            return Result<Dictionary<UsageMetric, long>>.Success(usageByMetric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all usage for clinic {ClinicId}", clinicId);
            return Result<Dictionary<UsageMetric, long>>.Failure($"An error occurred while getting usage: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsWithinLimitsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clinic = await _clinicRepository.GetWithSubscriptionAndPackageAsync(clinicId);

            if (clinic == null)
            {
                return Result<bool>.Failure($"Clinic {clinicId} not found");
            }

            if (clinic.CurrentSubscription == null)
            {
                return Result<bool>.Failure("No active subscription found");
            }

            var package = clinic.CurrentSubscription.Package;
            var currentPeriod = GetCurrentMonthPeriod();

            // Fix N+1: load ALL usage records for this clinic/period in one query,
            // then look up each metric in memory instead of firing one query per metric.
            var allUsageRecords = await _usageRecordRepository.GetByClinicAndPeriodAsync(clinicId, currentPeriod);
            var usageLookup = allUsageRecords.ToDictionary(u => u.Metric, u => u.Value);

            foreach (var metric in Enum.GetValues<UsageMetric>())
            {
                var currentUsage = usageLookup.GetValueOrDefault(metric, 0L);
                var limit = GetLimitForMetric(metric, package);

                if (limit.HasValue && currentUsage >= limit.Value)
                {
                    _logger.LogWarning("Clinic {ClinicId} exceeded limit for {Metric}: {Current}/{Limit}",
                        clinicId, metric, currentUsage, limit);
                    return Result<bool>.Success(false);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking limits for clinic {ClinicId}", clinicId);
            return Result<bool>.Failure($"An error occurred while checking limits: {ex.Message}");
        }
    }

    public async Task<Result> ResetMonthlyUsageAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recordsToDelete = await _usageRecordRepository.GetPreviousPeriodAsync(clinicId);

            foreach (var record in recordsToDelete)
            {
                await _usageRecordRepository.DeleteAsync(record);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Reset monthly usage for clinic {ClinicId}", clinicId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting usage for clinic {ClinicId}", clinicId);
            return Result.Failure($"An error occurred while resetting usage: {ex.Message}");
        }
    }

    private DateTime GetCurrentMonthPeriod()
    {
        var now = _clock.UtcNow;
        return new DateTime(now.Year, now.Month, 1);
    }

    private static bool IsWithinLimit(long currentValue, UsageMetric metric, Package package)
    {
        var limit = GetLimitForMetric(metric, package);
        return !limit.HasValue || currentValue < limit.Value;
    }

    private static long? GetLimitForMetric(UsageMetric metric, Package package)
    {
        var feature = package.Features.FirstOrDefault(f =>
            f.Feature?.Code == metric.ToString());

        return feature?.Quantity ?? 0;
    }
}
