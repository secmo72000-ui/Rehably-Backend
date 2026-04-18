using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Audit;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.DTOs.Usage;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Platform;

public class AuditLogService : IAuditLogService
{
    private readonly AuditDbContext _auditContext;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        AuditDbContext auditContext,
        IClinicRepository clinicRepository,
        IUserRepository userRepository,
        ILogger<AuditLogService> logger)
    {
        _auditContext = auditContext;
        _clinicRepository = clinicRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AuditLogListResponseDto>> GetAuditLogsAsync(AuditLogQueryDto query)
    {
        try
        {
            var logsQuery = _auditContext.AuditLogs.AsQueryable();

            if (query.ClinicId.HasValue)
                logsQuery = logsQuery.Where(a => a.TenantId == query.ClinicId.Value.ToString());

            if (query.UserId.HasValue)
                logsQuery = logsQuery.Where(a => a.UserId == query.UserId.Value.ToString());

            if (query.ActionType.HasValue)
                logsQuery = logsQuery.Where(a => a.ActionType == query.ActionType.Value.ToString());

            if (query.IsSuccess.HasValue)
                logsQuery = logsQuery.Where(a => a.IsSuccess == query.IsSuccess.Value);

            if (query.StartDate.HasValue)
                logsQuery = logsQuery.Where(a => a.Timestamp >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                logsQuery = logsQuery.Where(a => a.Timestamp <= query.EndDate.Value);

            var logs = await logsQuery
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var clinicGuids = logs
                .Where(l => l.TenantId != null && Guid.TryParse(l.TenantId, out _))
                .Select(l => Guid.Parse(l.TenantId!))
                .Distinct()
                .ToList();
            var clinicDict = await _clinicRepository.GetClinicNamesByIdsAsync(clinicGuids);

            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var userDict = await _userRepository.GetUserAuditInfoByIdsAsync(userIds);

            var filteredLogs = ApplyUserInfoFilters(logs, query, userDict);

            var totalCount = filteredLogs.Count;

            var pagedLogs = filteredLogs
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var items = pagedLogs.Select(log =>
            {
                userDict.TryGetValue(log.UserId, out var userInfo);
                Guid.TryParse(log.TenantId, out var clinicGuid);
                clinicDict.TryGetValue(clinicGuid, out var clinicName);

                return new AuditLogDto
                {
                    Id = log.Id,
                    Timestamp = log.Timestamp,
                    ActionType = log.ActionType,
                    ClinicId = Guid.TryParse(log.TenantId, out var cId) ? cId : (Guid?)null,
                    ClinicName = clinicName ?? (log.TenantId != null ? "Unknown" : "Platform"),
                    UserId = Guid.TryParse(log.UserId, out var userId) ? userId : Guid.Empty,
                    UserEmail = userInfo?.Email ?? "System",
                    UserRole = userInfo?.RoleName ?? "System",
                    EntityName = log.EntityName,
                    EntityId = Guid.TryParse(log.EntityId, out var entityId) ? entityId : Guid.Empty,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    IsSuccess = log.IsSuccess,
                    OtpReference = log.OtpReference
                };
            }).ToList();

            return Result<AuditLogListResponseDto>.Success(new AuditLogListResponseDto
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return Result<AuditLogListResponseDto>.Failure("Failed to retrieve audit logs");
        }
    }

    private static List<Domain.Entities.Audit.AuditLog> ApplyUserInfoFilters(
        List<Domain.Entities.Audit.AuditLog> logs,
        AuditLogQueryDto query,
        Dictionary<string, UserAuditInfoDto> userDict)
    {
        var result = logs;

        if (!string.IsNullOrEmpty(query.Email))
        {
            result = result
                .Where(l => userDict.TryGetValue(l.UserId, out var info) &&
                            info.Email != null &&
                            info.Email.Equals(query.Email, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrEmpty(query.Role))
        {
            result = result
                .Where(l => userDict.TryGetValue(l.UserId, out var info) &&
                            info.RoleName != null &&
                            info.RoleName.Equals(query.Role, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return result;
    }

    public async Task<Result<ClinicActivityDto>> GetClinicActivityAsync(Guid clinicId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var clinic = await _clinicRepository.GetWithSubscriptionAndPackageAsync(clinicId);

            if (clinic == null)
                return Result<ClinicActivityDto>.Failure("Clinic not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            var clinicIdStr = clinicId.ToString();

            var loginLogs = await _auditContext.AuditLogs
                .Where(a => a.TenantId == clinicIdStr &&
                           a.Timestamp >= start &&
                           a.Timestamp <= end &&
                           (a.ActionType == "Login" || a.ActionType == "Logout"))
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            var userIds = loginLogs.Select(l => l.UserId).Distinct().ToList();
            var userDict = await _userRepository.GetUserEmailsByIdsAsync(userIds);

            var dailyLogins = loginLogs
                .Where(l => l.ActionType == "Login")
                .Select(l => new LoginHistoryDto
                {
                    Date = l.Timestamp.Date,
                    Time = l.Timestamp.ToString("HH:mm"),
                    UserName = userDict.TryGetValue(l.UserId, out var email) ? email ?? "Unknown" : "Unknown",
                    Action = "Login"
                }).ToList();

            var failedLogins = await _auditContext.AuditLogs
                .Where(a => a.TenantId == clinicIdStr &&
                           a.Timestamp >= start &&
                           a.Timestamp <= end &&
                           a.ActionType == "LoginFailed")
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .Select(a => new FailedLoginDto
                {
                    Date = a.Timestamp.Date,
                    Time = a.Timestamp.ToString("HH:mm"),
                    UserIdentifier = a.UserId,
                    FailureReason = "Invalid credentials"
                })
                .ToListAsync();

            var patientCount = clinic.PatientsCount;
            var userCount = clinic.UsersCount;

            var package = clinic.CurrentSubscription?.Package;
            int? patientsLimit = null;
            int? usersLimit = null;

            if (package != null)
            {
                var patientFeature = package.Features.FirstOrDefault(f => f.Feature.Code == "max_patients" && f.IsIncluded);
                var userFeature = package.Features.FirstOrDefault(f => f.Feature.Code == "max_users" && f.IsIncluded);
                patientsLimit = patientFeature?.Quantity;
                usersLimit = userFeature?.Quantity;
            }

            var usage = new UsageStatisticsDto
            {
                PatientsUsed = patientCount,
                PatientsLimit = patientsLimit,
                PatientsPercentage = patientsLimit.HasValue && patientsLimit > 0
                    ? Math.Round((decimal)patientCount / patientsLimit.Value * 100, 1) : 0,
                UsersUsed = userCount,
                UsersLimit = usersLimit,
                UsersPercentage = usersLimit.HasValue && usersLimit > 0
                    ? Math.Round((decimal)userCount / usersLimit.Value * 100, 1) : 0,
                StorageUsedBytes = 0,
                StorageLimitBytes = 0,
                StoragePercentage = 0,
                OverallPercentage = 0
            };

            return Result<ClinicActivityDto>.Success(new ClinicActivityDto
            {
                ClinicId = clinicId,
                ClinicName = clinic.Name,
                PackageName = clinic.CurrentSubscription?.Package?.Name,
                Usage = usage,
                DailyLogins = dailyLogins,
                FailedLogins = failedLogins
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinic activity for clinic {ClinicId}", clinicId);
            return Result<ClinicActivityDto>.Failure("Failed to retrieve clinic activity");
        }
    }
}
