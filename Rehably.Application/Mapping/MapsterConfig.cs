using Mapster;
using Rehably.Application.DTOs.Library;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.DTOs.Package;
using Rehably.Application.DTOs.AddOn;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.DTOs.Invoice;
using Rehably.Domain.Entities.Library;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Application.Mapping;

public static class MapsterConfig
{
    public static void ConfigureMappings()
    {
        ConfigurePlatformMappings();
        ConfigureLibraryMappings();
        ConfigureTenantMappings();
    }

    private static void ConfigurePlatformMappings()
    {
        TypeAdapterConfig<Feature, FeatureDto>.NewConfig();

        TypeAdapterConfig<Feature, FeatureDetailDto>.NewConfig()
            .Map(dest => dest.Category, src => src.Category.Adapt<FeatureCategoryDto>());

        TypeAdapterConfig<FeatureCategory, FeatureCategoryDto>.NewConfig();

        TypeAdapterConfig<FeatureCategory, FeatureCategoryDetailDto>.NewConfig()
            .Map(dest => dest.SubCategories, src => src.SubCategories.Adapt<List<FeatureCategoryDto>>())
            .Map(dest => dest.Features, src => src.Features.Adapt<List<FeatureDto>>());

        TypeAdapterConfig<FeaturePricing, FeaturePricingDto>.NewConfig();

        TypeAdapterConfig<Package, PackageDto>.NewConfig()
            .Map(dest => dest.IsActive, src => src.Status == PackageStatus.Active)
            .Map(dest => dest.Features, src => src.Features
                .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted)
                .Select(pf => new PackageFeatureDto
                {
                    Id = pf.Id,
                    PackageId = pf.PackageId,
                    FeatureId = pf.FeatureId,
                    FeatureName = pf.Feature!.Name,
                    FeatureCode = pf.Feature.Code,
                    FeaturePrice = 0,
                    PricingType = pf.Feature.PricingType,
                    PerUnitPrice = 0,
                    IsIncluded = pf.IsIncluded,
                    Limit = pf.Quantity,
                    CalculatedPrice = pf.CalculatedPrice
                })
                .ToList());

        TypeAdapterConfig<Package, PublicPackageDto>.NewConfig()
            .Map(dest => dest.Tier, src => src.Tier.ToString())
            .Map(dest => dest.IsPopular, src => src.IsPopular)
            .Map(dest => dest.HasMonthly, src => src.MonthlyPrice > 0)
            .Map(dest => dest.HasYearly, src => src.YearlyPrice > 0)
            .Map(dest => dest.Features, src => src.Features
                .Where(pf => pf.Feature != null && !pf.Feature.IsDeleted && pf.IsIncluded)
                .Select(pf => new PublicPackageFeatureDto
                {
                    FeatureId = pf.FeatureId,
                    Name = pf.Feature!.Name,
                    Code = pf.Feature.Code,
                    Category = pf.Feature.Category != null ? pf.Feature.Category.Name : null,
                    Limit = pf.Quantity,
                    IconKey = pf.Feature.IconKey
                }).ToList());

        TypeAdapterConfig<SubscriptionAddOn, SubscriptionAddOnDto>.NewConfig()
            .Map(dest => dest.FeatureName, src => src.Feature != null ? src.Feature.Name : string.Empty)
            .Map(dest => dest.FeatureCode, src => src.Feature != null ? src.Feature.Code : string.Empty)
            .Map(dest => dest.Price, src => src.CalculatedPrice)
            .Map(dest => dest.NextBillingDate, src => src.EndDate);

        TypeAdapterConfig<SubscriptionAddOn, AddOnDto>.NewConfig()
            .Map(dest => dest.FeatureName, src => src.Feature != null ? src.Feature.Name : string.Empty)
            .Map(dest => dest.Limit, src => src.Quantity)
            .Map(dest => dest.Price, src => src.CalculatedPrice)
            .Map(dest => dest.PaymentType, src => src.Subscription != null ? src.Subscription.PaymentType : PaymentType.Cash);

        TypeAdapterConfig<Invoice, InvoiceDto>.NewConfig()
            .Map(dest => dest.Payments, src => src.Payments.Adapt<List<PaymentDto>>())
            .Map(dest => dest.LineItems, src => src.LineItems.Adapt<List<InvoiceLineItemDto>>());

        TypeAdapterConfig<InvoiceLineItem, InvoiceLineItemDto>.NewConfig();

        TypeAdapterConfig<Payment, PaymentDto>.NewConfig()
            .Map(dest => dest.Provider, src => src.Provider.ToString());
    }

    private static void ConfigureLibraryMappings()
    {
        TypeAdapterConfig<Treatment, TreatmentDto>.NewConfig()
            .Map(dest => dest.BodyRegionCategoryName, src => src.BodyRegionCategory != null ? src.BodyRegionCategory.Name : null)
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Ignore(dest => dest.IsGlobal);

        TypeAdapterConfig<Exercise, ExerciseDto>.NewConfig()
            .Map(dest => dest.BodyRegionCategoryName, src => src.BodyRegionCategory != null ? src.BodyRegionCategory.Name : null)
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Ignore(dest => dest.IsGlobal);

        TypeAdapterConfig<Modality, ModalityDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Ignore(dest => dest.IsGlobal);

        TypeAdapterConfig<Assessment, AssessmentDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Ignore(dest => dest.IsGlobal);

        TypeAdapterConfig<Device, DeviceDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Ignore(dest => dest.IsGlobal);

        TypeAdapterConfig<ClinicLibraryOverride, ClinicLibraryOverrideDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted);

        TypeAdapterConfig<TreatmentStage, TreatmentStageDto>.NewConfig()
            .Map(dest => dest.BodyRegionName, src => src.BodyRegion != null ? src.BodyRegion.Name : null)
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);
    }

    private static void ConfigureTenantMappings()
    {
        TypeAdapterConfig<ClinicDocument, ClinicDocumentDto>.NewConfig()
            .Map(dest => dest.Type, src => src.DocumentType.ToString())
            .Map(dest => dest.FileUrl, src => src.PublicUrl ?? src.StorageUrl)
            .Map(dest => dest.VerificationStatus, src => src.Status.ToString());

        TypeAdapterConfig<Clinic, ClinicResponse>.NewConfig()
            .Map(dest => dest.IsBanned, src => src.Status == ClinicStatus.Banned)
            .Map(dest => dest.SubscriptionPlanId, src => src.CurrentSubscription != null ? src.CurrentSubscription.PackageId : (Guid?)null)
            .Map(dest => dest.SubscriptionPlanName, src => src.CurrentSubscription != null && src.CurrentSubscription.Package != null ? src.CurrentSubscription.Package.Name : null)
            .Map(dest => dest.SubscriptionStatus, src => src.CurrentSubscription != null ? src.CurrentSubscription.Status : SubscriptionStatus.Expired)
            .Map(dest => dest.SubscriptionStartDate, src => src.CurrentSubscription != null ? src.CurrentSubscription.StartDate : DateTime.MinValue)
            .Map(dest => dest.SubscriptionEndDate, src => src.CurrentSubscription != null ? src.CurrentSubscription.EndDate : (DateTime?)null)
            .Map(dest => dest.TrialEndDate, src => src.CurrentSubscription != null ? src.CurrentSubscription.TrialEndsAt : (DateTime?)null)
            .Map(dest => dest.PaymentMethod, src => src.CurrentSubscription != null ? src.CurrentSubscription.PaymentType.ToString() : null)
            .Map(dest => dest.PackageFeatures, src => src.CurrentSubscription != null && src.CurrentSubscription.Package != null && src.CurrentSubscription.Package.Features != null
                ? src.CurrentSubscription.Package.Features
                    .Where(pf => pf.IsIncluded && pf.Feature != null)
                    .Select(pf => new ClinicSubscriptionFeatureDto
                    {
                        FeatureId = pf.FeatureId,
                        Name = pf.Feature.Name,
                        Code = pf.Feature.Code,
                        IsIncluded = pf.IsIncluded,
                        Limit = pf.Quantity ?? pf.Limit
                    }).ToList()
                : new List<ClinicSubscriptionFeatureDto>())
            .Map(dest => dest.Documents, src => src.Documents != null
                ? src.Documents.Adapt<List<ClinicDocumentDto>>()
                : new List<ClinicDocumentDto>());
    }
}
