using Microsoft.EntityFrameworkCore;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Billing;
using Rehably.Application.Services.Billing;
using Rehably.Domain.Entities.Billing;
using Rehably.Infrastructure.Data;

namespace Rehably.Infrastructure.Services.Billing;

public class DiscountService : IDiscountService
{
    private readonly ApplicationDbContext _db;
    public DiscountService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DiscountDto>> GetDiscountsAsync(Guid clinicId, DiscountQueryParams query)
    {
        var q = _db.Discounts.Include(d => d.PackageOffer).Where(d => d.ClinicId == clinicId);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(d => d.Name.Contains(query.Search) || (d.Code != null && d.Code.Contains(query.Search)));
        if (query.Type.HasValue) q = q.Where(d => d.Type == query.Type);
        if (query.IsActive.HasValue) q = q.Where(d => d.IsActive == query.IsActive);
        if (query.Method.HasValue) q = q.Where(d => d.ApplicationMethod == query.Method);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(d => d.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(d => MapDiscount(d)).ToListAsync();
        return new PagedResult<DiscountDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<DiscountDto?> GetDiscountByIdAsync(Guid clinicId, Guid id)
    {
        var e = await _db.Discounts.Include(d => d.PackageOffer)
            .FirstOrDefaultAsync(d => d.ClinicId == clinicId && d.Id == id);
        return e == null ? null : MapDiscount(e);
    }

    public async Task<DiscountDto> CreateDiscountAsync(Guid clinicId, CreateDiscountRequest request)
    {
        var entity = new Discount
        {
            Id = Guid.NewGuid(), ClinicId = clinicId,
            Name = request.Name, NameArabic = request.NameArabic,
            Code = request.Code, Type = request.Type, Value = request.Value,
            AppliesTo = request.AppliesTo, ApplicationMethod = request.ApplicationMethod,
            AutoCondition = request.AutoCondition, IsActive = true,
            StartsAt = request.StartsAt, ExpiresAt = request.ExpiresAt,
            MaxUsageTotal = request.MaxUsageTotal, MaxUsagePerPatient = request.MaxUsagePerPatient
        };

        if (request.PackageOffer != null)
            entity.PackageOffer = new SessionPackageOffer
            {
                Id = Guid.NewGuid(), DiscountId = entity.Id,
                SessionsToPurchase = request.PackageOffer.SessionsToPurchase,
                SessionsFree = request.PackageOffer.SessionsFree,
                ValidForServiceType = request.PackageOffer.ValidForServiceType
            };

        _db.Discounts.Add(entity);
        await _db.SaveChangesAsync();
        return MapDiscount(entity);
    }

    public async Task<DiscountDto> UpdateDiscountAsync(Guid clinicId, Guid id, UpdateDiscountRequest request)
    {
        var entity = await _db.Discounts.Include(d => d.PackageOffer)
            .FirstOrDefaultAsync(d => d.ClinicId == clinicId && d.Id == id)
            ?? throw new KeyNotFoundException("Discount not found");

        entity.Name = request.Name; entity.NameArabic = request.NameArabic;
        entity.Code = request.Code; entity.Type = request.Type; entity.Value = request.Value;
        entity.AppliesTo = request.AppliesTo; entity.ApplicationMethod = request.ApplicationMethod;
        entity.AutoCondition = request.AutoCondition; entity.IsActive = request.IsActive;
        entity.StartsAt = request.StartsAt; entity.ExpiresAt = request.ExpiresAt;
        entity.MaxUsageTotal = request.MaxUsageTotal; entity.MaxUsagePerPatient = request.MaxUsagePerPatient;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapDiscount(entity);
    }

    public async Task DeleteDiscountAsync(Guid clinicId, Guid id)
    {
        var entity = await _db.Discounts.FirstOrDefaultAsync(d => d.ClinicId == clinicId && d.Id == id)
            ?? throw new KeyNotFoundException("Discount not found");
        _db.Discounts.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<ValidateDiscountResponse> ValidateCodeAsync(Guid clinicId, ValidateDiscountRequest request)
    {
        var discount = await _db.Discounts
            .FirstOrDefaultAsync(d => d.ClinicId == clinicId && d.Code == request.Code && d.IsActive);

        if (discount == null)
            return new ValidateDiscountResponse(false, "كود الخصم غير صالح", null, 0);

        var now = DateTime.UtcNow;
        if (discount.StartsAt.HasValue && discount.StartsAt > now)
            return new ValidateDiscountResponse(false, "الخصم لم يبدأ بعد", null, 0);
        if (discount.ExpiresAt.HasValue && discount.ExpiresAt < now)
            return new ValidateDiscountResponse(false, "انتهت صلاحية الخصم", null, 0);
        if (discount.MaxUsageTotal.HasValue && discount.UsageCount >= discount.MaxUsageTotal)
            return new ValidateDiscountResponse(false, "تجاوز الحد الأقصى لاستخدام الخصم", null, 0);

        if (discount.MaxUsagePerPatient.HasValue && request.PatientId.HasValue)
        {
            var patientUsage = await _db.DiscountUsages.CountAsync(u => u.DiscountId == discount.Id && u.PatientId == request.PatientId);
            if (patientUsage >= discount.MaxUsagePerPatient)
                return new ValidateDiscountResponse(false, "تجاوزت الحد الأقصى لاستخدام هذا الخصم", null, 0);
        }

        var amount = discount.Type == Domain.Enums.DiscountType.Percentage
            ? request.SubTotal * discount.Value / 100
            : discount.Value;

        return new ValidateDiscountResponse(true, null, discount.Id, Math.Min(amount, request.SubTotal));
    }

    public async Task<PagedResult<DiscountUsageDto>> GetUsagesAsync(Guid clinicId, Guid discountId, int page, int pageSize)
    {
        var q = _db.DiscountUsages.Where(u => u.DiscountId == discountId && u.Discount.ClinicId == clinicId);
        var total = await q.CountAsync();
        var items = await q.Include(u => u.Discount)
            .OrderByDescending(u => u.AppliedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new DiscountUsageDto(u.Id, u.DiscountId, u.Discount.Name,
                u.PatientId, string.Empty, u.InvoiceId, string.Empty,
                u.AmountApplied, u.AppliedAt, u.AppliedByUserId))
            .ToListAsync();
        return new PagedResult<DiscountUsageDto>(items, total, page, pageSize);
    }

    private static DiscountDto MapDiscount(Discount d) =>
        new(d.Id, d.Name, d.NameArabic, d.Code, d.Type, d.Value,
            d.AppliesTo, d.ApplicationMethod, d.AutoCondition, d.IsActive,
            d.StartsAt, d.ExpiresAt, d.MaxUsageTotal, d.MaxUsagePerPatient,
            d.UsageCount, d.TotalValueGiven,
            d.PackageOffer == null ? null : new SessionPackageOfferDto(
                d.PackageOffer.Id, d.PackageOffer.SessionsToPurchase,
                d.PackageOffer.SessionsFree, d.PackageOffer.ValidForServiceType));
}
