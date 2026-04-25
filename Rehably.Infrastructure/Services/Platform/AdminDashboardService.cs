using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Platform;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardService(ApplicationDbContext db) => _db = db;

    public async Task<Result<AdminDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            // Parallel counts
            var totalClinicsTask        = _db.Clinics.CountAsync(ct);
            var activeClinicsTask       = _db.Clinics.CountAsync(c => c.Status == ClinicStatus.Active, ct);
            var suspendedClinicsTask    = _db.Clinics.CountAsync(c => c.Status == ClinicStatus.Suspended, ct);
            var totalUsersTask          = _db.Users.CountAsync(ct);
            var activeSubsTask          = _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active, ct);
            var revenueTask             = _db.Payments
                                            .Where(p => p.Status == PaymentStatus.Completed)
                                            .SumAsync(p => (decimal?)p.Amount, ct);
            var recentSubsTask          = _db.Subscriptions
                                            .Include(s => s.Clinic)
                                            .Include(s => s.Package)
                                            .OrderByDescending(s => s.CreatedAt)
                                            .Take(8)
                                            .Select(s => new RecentSubscriptionItem(
                                                s.Id,
                                                s.ClinicId,
                                                s.Clinic != null ? s.Clinic.Name : "—",
                                                s.Package != null ? s.Package.Name : "—",
                                                s.Status,
                                                s.StartDate,
                                                s.EndDate,
                                                s.CreatedAt
                                            ))
                                            .ToListAsync(ct);

            await Task.WhenAll(
                totalClinicsTask, activeClinicsTask, suspendedClinicsTask,
                totalUsersTask, activeSubsTask, revenueTask, recentSubsTask);

            var dto = new AdminDashboardDto(
                TotalClinics:        totalClinicsTask.Result,
                ActiveClinics:       activeClinicsTask.Result,
                SuspendedClinics:    suspendedClinicsTask.Result,
                TotalUsers:          totalUsersTask.Result,
                ActiveSubscriptions: activeSubsTask.Result,
                TotalRevenue:        revenueTask.Result ?? 0m,
                RecentSubscriptions: recentSubsTask.Result
            );

            return Result<AdminDashboardDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<AdminDashboardDto>.Failure($"Failed to load admin dashboard: {ex.Message}");
        }
    }
}
