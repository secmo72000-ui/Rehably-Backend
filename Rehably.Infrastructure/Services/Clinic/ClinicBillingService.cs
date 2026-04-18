using Hangfire;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Auth;
using Rehably.Application.Services.Clinic;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.BackgroundJobs;

namespace Rehably.Infrastructure.Services.Clinic;

/// <summary>
/// Implementation of clinic billing and status operations.
/// </summary>
public class ClinicBillingService : IClinicBillingService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ClinicBillingService> _logger;

    public ClinicBillingService(
        IClinicRepository clinicRepository,
        IPackageRepository packageRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<ClinicBillingService> logger)
    {
        _clinicRepository = clinicRepository;
        _packageRepository = packageRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Result> UpgradeSubscriptionAsync(Guid clinicId, Guid newPlanId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            var newPlan = await _packageRepository.GetByIdAsync(newPlanId);
            if (newPlan == null)
            {
                return Result.Failure("New package not found");
            }

            clinic.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result> SuspendClinicAsync(Guid clinicId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            clinic.Status = ClinicStatus.Suspended;
            clinic.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result> ActivateClinicAsync(Guid clinicId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            clinic.Status = ClinicStatus.Active;
            clinic.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Result> BanClinicAsync(Guid clinicId, string reason, string adminUserId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            if (clinic.IsBanned)
            {
                return Result.Failure("Clinic is already banned");
            }

            clinic.Ban(reason, adminUserId);
            clinic.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await _tokenService.InvalidateClinicTokensAsync(clinicId);

            if (!string.IsNullOrEmpty(clinic.Email))
            {
                try
                {
                    SendBanNotificationEmailAsync(clinic.Email, clinic.Name, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send ban notification email to {Email}", clinic.Email);
                }
            }

            await _unitOfWork.CommitTransactionAsync();
            _logger.LogInformation("Clinic {ClinicId} banned by admin {AdminId}. Reason: {Reason}", clinicId, adminUserId, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to ban clinic {ClinicId}", clinicId);
            return Result.Failure($"Failed to ban clinic: {ex.Message}");
        }
    }

    public async Task<Result> UnbanClinicAsync(Guid clinicId, string? reason, string adminUserId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var clinic = await _clinicRepository.GetByIdAsync(clinicId);
            if (clinic == null)
            {
                return Result.Failure("Clinic not found");
            }

            if (!clinic.IsBanned)
            {
                return Result.Failure("Clinic is not banned");
            }

            clinic.BanReason = null;
            clinic.BannedAt = null;
            clinic.BannedBy = null;

            var subscription = await _subscriptionRepository.GetActiveSubscriptionByClinicIdAsync(clinic.Id);
            clinic.Status = subscription != null ? ClinicStatus.Active : ClinicStatus.Suspended;

            clinic.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Clinic {ClinicId} unbanned by admin {AdminId}. Reason: {Reason}", clinicId, adminUserId, reason ?? "Not specified");

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to unban clinic {ClinicId}", clinicId);
            return Result.Failure($"Failed to unban clinic: {ex.Message}");
        }
    }

    private void SendBanNotificationEmailAsync(string email, string clinicName, string reason)
    {
        _backgroundJobClient.Enqueue<SendClinicBanNotificationJob>(job =>
            job.ExecuteAsync(email, clinicName, reason));

        _logger.LogInformation("Ban notification email queued for {Email} for clinic {ClinicName}", email, clinicName);
    }
}
