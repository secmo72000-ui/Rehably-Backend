using Microsoft.EntityFrameworkCore;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Rehably.Infrastructure.Data;
using PackageTier = Rehably.Domain.Enums.PackageTier;

namespace Rehably.Infrastructure.Seed;

public static class SubscriptionPackageSeeder
{
    public static async Task SeedSubscriptionPackageSystemAsync(ApplicationDbContext context)
    {
        await SeedFeaturesAsync(context);
        await context.SaveChangesAsync();

        await SeedFeaturePricingAsync(context);
        await context.SaveChangesAsync();

        await SeedPackagesAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFeaturesAsync(ApplicationDbContext context)
    {
        if (await context.Features.AnyAsync()) return;

        var categories = await context.FeatureCategories.ToListAsync();
        if (!categories.Any())
        {
            var coreCategory = new FeatureCategory
            {
                Name = "Core Features",
                Code = "core",
                Description = "Essential features for clinic operations",
                IsActive = true,
                DisplayOrder = 1
            };
            context.FeatureCategories.Add(coreCategory);
            await context.SaveChangesAsync();
            categories.Add(coreCategory);

            var communicationCategory = new FeatureCategory
            {
                Name = "Communication",
                Code = "communication",
                Description = "Messaging and communication features",
                IsActive = true,
                DisplayOrder = 2
            };
            context.FeatureCategories.Add(communicationCategory);
            await context.SaveChangesAsync();
            categories.Add(communicationCategory);
        }

        var coreCategoryId = categories.FirstOrDefault(c => c.Code == "core")?.Id ?? categories.First().Id;
        var communicationCategoryId = categories.FirstOrDefault(c => c.Code == "communication")?.Id ?? categories.Last().Id;

        var features = new List<Feature>
        {
            new Feature
            {
                CategoryId = coreCategoryId,
                Code = "users",
                Name = "Users",
                Description = "Number of user accounts allowed in clinic",
                PricingType = PricingType.PerUser,
                IsActive = true,
                DisplayOrder = 1
            },
            new Feature
            {
                CategoryId = coreCategoryId,
                Code = "storage",
                Name = "Storage",
                Description = "Storage space in GB for documents and files",
                PricingType = PricingType.PerStorageGB,
                IsActive = true,
                DisplayOrder = 2
            },
            new Feature
            {
                CategoryId = coreCategoryId,
                Code = "patients",
                Name = "Patients",
                Description = "Number of patient records",
                PricingType = PricingType.PerUnit,
                IsActive = true,
                DisplayOrder = 3
            },
            new Feature
            {
                CategoryId = communicationCategoryId,
                Code = "sms",
                Name = "SMS",
                Description = "SMS messages per month for appointments and reminders",
                PricingType = PricingType.PerUnit,
                IsActive = true,
                DisplayOrder = 4
            },
            new Feature
            {
                CategoryId = communicationCategoryId,
                Code = "whatsapp",
                Name = "WhatsApp",
                Description = "WhatsApp messages per month for appointments and reminders",
                PricingType = PricingType.PerUnit,
                IsActive = true,
                DisplayOrder = 5
            },
            new Feature
            {
                CategoryId = communicationCategoryId,
                Code = "email",
                Name = "Email",
                Description = "Email notifications per month",
                PricingType = PricingType.PerUnit,
                IsActive = true,
                DisplayOrder = 6
            }
        };

        await context.Features.AddRangeAsync(features);
    }

    private static async Task SeedFeaturePricingAsync(ApplicationDbContext context)
    {
        if (await context.FeaturePricings.AnyAsync()) return;

        var features = await context.Features.ToListAsync();

        var pricingDefs = new[]
        {
            (Code: "users",    BasePrice: 0m, PerUnitPrice: 1m),
            (Code: "storage",  BasePrice: 0m, PerUnitPrice: 0.5m),
            (Code: "patients", BasePrice: 0m, PerUnitPrice: 0.1m),
            (Code: "sms",      BasePrice: 0m, PerUnitPrice: 0.01m),
            (Code: "whatsapp", BasePrice: 0m, PerUnitPrice: 0.02m),
            (Code: "email",    BasePrice: 0m, PerUnitPrice: 0.001m)
        };

        var pricing = new List<FeaturePricing>();
        foreach (var def in pricingDefs)
        {
            var feature = features.FirstOrDefault(f => f.Code == def.Code);
            if (feature is null) continue;

            pricing.Add(new FeaturePricing
            {
                FeatureId = feature.Id,
                BasePrice = def.BasePrice,
                PerUnitPrice = def.PerUnitPrice,
                EffectiveDate = DateTime.UtcNow
            });
        }

        if (pricing.Count > 0)
            await context.FeaturePricings.AddRangeAsync(pricing);
    }

    private static async Task SeedPackagesAsync(ApplicationDbContext context)
    {
        if (await context.Packages.AnyAsync()) return;

        var features = await context.Features.ToListAsync();
        var usersFeature = features.FirstOrDefault(f => f.Code == "users");
        var storageFeature = features.FirstOrDefault(f => f.Code == "storage");
        var patientsFeature = features.FirstOrDefault(f => f.Code == "patients");
        var smsFeature = features.FirstOrDefault(f => f.Code == "sms");
        var whatsappFeature = features.FirstOrDefault(f => f.Code == "whatsapp");
        var emailFeature = features.FirstOrDefault(f => f.Code == "email");

        FeaturePricing? GetPricing(Feature? f) => f is null ? null : context.FeaturePricings
            .Where(p => p.FeatureId == f.Id)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault();

        var usersPricing = GetPricing(usersFeature);
        var storagePricing = GetPricing(storageFeature);
        var patientsPricing = GetPricing(patientsFeature);
        var smsPricing = GetPricing(smsFeature);
        var whatsappPricing = GetPricing(whatsappFeature);
        var emailPricing = GetPricing(emailFeature);

        var packages = new List<Package>
        {
            new Package
            {
                Name = "Starter",
                Code = "starter",
                Description = "Essential features for small clinics",
                MonthlyPrice = 299m,
                YearlyPrice = 2990m,
                CalculatedMonthlyPrice = 24m,
                CalculatedYearlyPrice = 240m,
                IsPublic = true,
                IsCustom = false,
                Status = PackageStatus.Active,
                Tier = PackageTier.Basic,
                IsPopular = false,
                DisplayOrder = 1,
                TrialDays = 14,
                Features = BuildPackageFeatures(
                    (usersFeature,   5,      usersPricing),
                    (storageFeature, 10,     storagePricing),
                    (patientsFeature, 100,   patientsPricing),
                    (smsFeature,     100,    smsPricing),
                    (emailFeature,   500,    emailPricing)
                )
            },
            new Package
            {
                Name = "Pro",
                Code = "pro",
                Description = "Advanced features for growing clinics",
                MonthlyPrice = 599m,
                YearlyPrice = 5990m,
                CalculatedMonthlyPrice = 60m,
                CalculatedYearlyPrice = 600m,
                IsPublic = true,
                IsCustom = false,
                Status = PackageStatus.Active,
                Tier = PackageTier.Standard,
                IsPopular = true,
                DisplayOrder = 2,
                TrialDays = 14,
                Features = BuildPackageFeatures(
                    (usersFeature,    20,   usersPricing),
                    (storageFeature,  20,   storagePricing),
                    (patientsFeature, 500,  patientsPricing),
                    (smsFeature,      500,  smsPricing),
                    (whatsappFeature, 250,  whatsappPricing),
                    (emailFeature,    2000, emailPricing)
                )
            },
            new Package
            {
                Name = "Enterprise",
                Code = "enterprise",
                Description = "Complete solution for large clinics and hospitals",
                MonthlyPrice = 1299m,
                YearlyPrice = 12990m,
                CalculatedMonthlyPrice = 152m,
                CalculatedYearlyPrice = 1520m,
                IsPublic = true,
                IsCustom = false,
                Status = PackageStatus.Active,
                Tier = PackageTier.Enterprise,
                IsPopular = false,
                DisplayOrder = 3,
                TrialDays = 14,
                Features = BuildPackageFeatures(
                    (usersFeature,    50,     usersPricing),
                    (storageFeature,  100,    storagePricing),
                    (patientsFeature, 999999, patientsPricing),
                    (smsFeature,      5000,   smsPricing),
                    (whatsappFeature, 2500,   whatsappPricing),
                    (emailFeature,    10000,  emailPricing)
                )
            }
        };

        await context.Packages.AddRangeAsync(packages);
    }

    private static List<PackageFeature> BuildPackageFeatures(
        params (Feature? Feature, int Quantity, FeaturePricing? Pricing)[] entries)
    {
        var result = new List<PackageFeature>();
        foreach (var (feature, quantity, pricing) in entries)
        {
            if (feature is null) continue;
            result.Add(new PackageFeature
            {
                FeatureId = feature.Id,
                Quantity = quantity,
                IsIncluded = true,
                CalculatedPrice = quantity * (pricing?.PerUnitPrice ?? 0)
            });
        }
        return result;
    }
}
