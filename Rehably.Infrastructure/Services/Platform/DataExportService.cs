using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.Contexts;
using Rehably.Application.Services.Platform;
using Rehably.Infrastructure.Data;
using System.IO.Compression;
using System.Text;

namespace Rehably.Infrastructure.Services.Platform;

public class DataExportService : IDataExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DataExportService> _logger;

    public DataExportService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<DataExportService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<byte[]>> ExportClinicDataAsync(Guid clinicId)
    {
        // ── IDOR Protection ────────────────────────────────────────────────────────
        // If the caller has a tenant context (i.e. is a Clinic Admin / Staff),
        // they may only export their OWN clinic's data.
        // Platform Admins have no TenantId in the JWT, so _tenantContext.TenantId is null
        // and are allowed to export any clinic.
        if (_tenantContext.TenantId.HasValue && _tenantContext.TenantId.Value != clinicId)
        {
            _logger.LogWarning(
                "IDOR attempt blocked: caller tenant {CallerTenant} tried to export data for clinic {TargetClinic}",
                _tenantContext.TenantId, clinicId);
            return Result<byte[]>.Failure("Access denied: you can only export your own clinic's data");
        }
        // ──────────────────────────────────────────────────────────────────────────

        var clinic = await _context.Clinics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == clinicId);

        if (clinic == null)
            return Result<byte[]>.Failure("Clinic not found");

        try
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                await WriteSubscriptionsCsvAsync(archive, clinicId);
                await WriteInvoicesCsvAsync(archive, clinicId);
                await WriteUsageCsvAsync(archive, clinicId);
            }

            var bytes = memoryStream.ToArray();

            if (bytes.Length == 0)
                return Result<byte[]>.Failure("Export produced no data");

            _logger.LogInformation("Exported data for clinic {ClinicId}: {Bytes} bytes", clinicId, bytes.Length);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data for clinic {ClinicId}", clinicId);
            return Result<byte[]>.Failure("Export failed: " + ex.Message);
        }
    }

    private async Task WriteSubscriptionsCsvAsync(ZipArchive archive, Guid clinicId)
    {
        var subscriptions = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.ClinicId == clinicId)
            .Include(s => s.Package)
            .ToListAsync();

        var entry = archive.CreateEntry("subscriptions.csv");
        await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);

        await writer.WriteLineAsync("Id,PackageName,Status,BillingCycle,StartDate,EndDate,TrialEndsAt,PaymentType,CreatedAt");

        foreach (var s in subscriptions)
        {
            await writer.WriteLineAsync(
                $"{s.Id},{EscapeCsv(s.Package?.Name ?? string.Empty)},{s.Status},{s.BillingCycle}," +
                $"{s.StartDate:yyyy-MM-dd},{s.EndDate:yyyy-MM-dd},{s.TrialEndsAt?.ToString("yyyy-MM-dd") ?? string.Empty}," +
                $"{s.PaymentType},{s.CreatedAt:yyyy-MM-dd}");
        }
    }

    private async Task WriteInvoicesCsvAsync(ZipArchive archive, Guid clinicId)
    {
        var invoices = await _context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.ClinicId == clinicId)
            .ToListAsync();

        var entry = archive.CreateEntry("invoices.csv");
        await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);

        await writer.WriteLineAsync("Id,InvoiceNumber,Amount,TaxAmount,TotalAmount,DueDate,PaidAt,CreatedAt");

        foreach (var inv in invoices)
        {
            await writer.WriteLineAsync(
                $"{inv.Id},{EscapeCsv(inv.InvoiceNumber)},{inv.Amount},{inv.TaxAmount},{inv.TotalAmount}," +
                $"{inv.DueDate:yyyy-MM-dd},{inv.PaidAt?.ToString("yyyy-MM-dd") ?? string.Empty},{inv.CreatedAt:yyyy-MM-dd}");
        }
    }

    private async Task WriteUsageCsvAsync(ZipArchive archive, Guid clinicId)
    {
        var subscriptionIds = await _context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.ClinicId == clinicId)
            .Select(s => s.Id)
            .ToListAsync();

        var usages = await _context.SubscriptionFeatureUsages
            .Where(u => subscriptionIds.Contains(u.SubscriptionId))
            .Include(u => u.Feature)
            .ToListAsync();

        var entry = archive.CreateEntry("feature_usage.csv");
        await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);

        await writer.WriteLineAsync("Id,SubscriptionId,FeatureCode,FeatureName,Used,Limit,LastResetAt");

        foreach (var u in usages)
        {
            await writer.WriteLineAsync(
                $"{u.Id},{u.SubscriptionId},{EscapeCsv(u.Feature?.Code ?? string.Empty)}," +
                $"{EscapeCsv(u.Feature?.Name ?? string.Empty)},{u.Used},{u.Limit}," +
                $"{u.LastResetAt:yyyy-MM-dd}");
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
