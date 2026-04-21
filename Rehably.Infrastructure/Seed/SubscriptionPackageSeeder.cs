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
        // Ensure categories exist
        var categories = await context.FeatureCategories.ToListAsync();
        if (!categories.Any(c => c.Code == "core"))
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
        }
        if (!categories.Any(c => c.Code == "communication"))
        {
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

        var coreCategoryId = categories.First(c => c.Code == "core").Id;
        var communicationCategoryId = categories.First(c => c.Code == "communication").Id;

        // Idempotent: only add features that don't exist yet (by code)
        var existingCodes = await context.Features.Select(f => f.Code).ToListAsync();

        var featureDefs = new List<(string Code, string Name, string Description, PricingType Pricing, int Order, Guid CategoryId)>
        {
            ("users",        "Users",        "Number of user accounts allowed in clinic",              PricingType.PerUser,      1, coreCategoryId),
            ("storage",      "Storage",      "Storage space in GB for documents and files",            PricingType.PerStorageGB, 2, coreCategoryId),
            ("patients",     "Patients",     "Number of patient records",                              PricingType.PerUnit,      3, coreCategoryId),
            ("appointments", "Appointments", "Scheduling appointments and calendar management",        PricingType.PerUnit,      4, coreCategoryId),
            ("sessions",     "Sessions",     "Treatment sessions per month",                           PricingType.PerUnit,      5, coreCategoryId),
            ("invoices",     "Invoices",     "Billing and invoice management",                         PricingType.PerUnit,      6, coreCategoryId),
            ("payments",     "Payments",     "Online payment processing",                              PricingType.PerUnit,      7, coreCategoryId),
            ("reports",      "Reports",      "Analytics and reporting dashboard",                      PricingType.PerUnit,      8, coreCategoryId),
            ("sms",          "SMS",          "SMS messages per month for appointments and reminders",  PricingType.PerUnit,      9,  communicationCategoryId),
            ("whatsapp",     "WhatsApp",     "WhatsApp messages per month",                            PricingType.PerUnit,      10, communicationCategoryId),
            ("email",        "Email",        "Email notifications per month",                          PricingType.PerUnit,      11, communicationCategoryId),
        };

        var toAdd = featureDefs
            .Where(f => !existingCodes.Contains(f.Code))
            .Select(f => new Feature
            {
                CategoryId = f.CategoryId,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                PricingType = f.Pricing,
                IsActive = true,
                DisplayOrder = f.Order
            }).ToList();

        if (toAdd.Count > 0)
            await context.Features.AddRangeAsync(toAdd);
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
        var features = await context.Features.ToListAsync();

        Feature? F(string code) => features.FirstOrDefault(f => f.Code == code);
        FeaturePricing? P(Feature? f) => f is null ? null : context.FeaturePricings
            .Where(p => p.FeatureId == f.Id).OrderByDescending(p => p.EffectiveDate).FirstOrDefault();

        // Core functional features (unlimited per-plan, just IsIncluded=true, quantity=999999)
        var unlimited = 999_999;

        var packageDefs = new[]
        {
            new
            {
                Code = "starter", Name = "Starter", Desc = "Essential features for small clinics",
                Monthly = 299m, Yearly = 2990m, CalcMonthly = 24m, CalcYearly = 240m,
                Tier = PackageTier.Basic, Popular = false, Order = 1,
                FeatureQuotas = new[] {
                    ("users", 5), ("storage", 10), ("patients", 100),
                    ("appointments", unlimited), ("sessions", unlimited), ("invoices", unlimited),
                    ("payments", unlimited), ("reports", unlimited),
                    ("sms", 100), ("email", 500), ("whatsapp", 0)
                }
            },
            new
            {
                Code = "pro", Name = "Pro", Desc = "Advanced features for growing clinics",
                Monthly = 599m, Yearly = 5990m, CalcMonthly = 60m, CalcYearly = 600m,
                Tier = PackageTier.Standard, Popular = true, Order = 2,
                FeatureQuotas = new[] {
                    ("users", 20), ("storage", 20), ("patients", 500),
                    ("appointments", unlimited), ("sessions", unlimited), ("invoices", unlimited),
                    ("payments", unlimited), ("reports", unlimited),
                    ("sms", 500), ("email", 2000), ("whatsapp", 250)
                }
            },
            new
            {
                Code = "enterprise", Name = "Enterprise", Desc = "Complete solution for large clinics and hospitals",
                Monthly = 1299m, Yearly = 12990m, CalcMonthly = 152m, CalcYearly = 1520m,
                Tier = PackageTier.Enterprise, Popular = false, Order = 3,
                FeatureQuotas = new[] {
                    ("users", 50), ("storage", 100), ("patients", unlimited),
                    ("appointments", unlimited), ("sessions", unlimited), ("invoices", unlimited),
                    ("payments", unlimited), ("reports", unlimited),
                    ("sms", 5000), ("email", 10000), ("whatsapp", 2500)
                }
            }
        };

        var existingPackageCodes = await context.Packages.Select(p => p.Code).ToListAsync();

        foreach (var def in packageDefs)
        {
            if (existingPackageCodes.Contains(def.Code))
            {
                // Package exists — patch any missing PackageFeatures
                var pkg = await context.Packages
                    .Include(p => p.Features)
                    .FirstAsync(p => p.Code == def.Code);

                var existingFeatureCodes = pkg.Features
                    .Select(pf => features.FirstOrDefault(f => f.Id == pf.FeatureId)?.Code)
                    .Where(c => c != null).ToHashSet();

                foreach (var (fCode, qty) in def.FeatureQuotas)
                {
                    if (existingFeatureCodes.Contains(fCode)) continue;
                    var feat = F(fCode);
                    if (feat is null) continue;
                    pkg.Features.Add(new PackageFeature
                    {
                        FeatureId = feat.Id,
                        Quantity = qty,
                        IsIncluded = qty > 0,
                        CalculatedPrice = qty * (P(feat)?.PerUnitPrice ?? 0)
                    });
                }
                continue;
            }

            // New package
            var packageFeatures = new List<PackageFeature>();
            foreach (var (fCode, qty) in def.FeatureQuotas)
            {
                var feat = F(fCode);
                if (feat is null) continue;
                packageFeatures.Add(new PackageFeature
                {
                    FeatureId = feat.Id,
                    Quantity = qty,
                    IsIncluded = qty > 0,
                    CalculatedPrice = qty * (P(feat)?.PerUnitPrice ?? 0)
                });
            }

            context.Packages.Add(new Package
            {
                Name = def.Name,
                Code = def.Code,
                Description = def.Desc,
                MonthlyPrice = def.Monthly,
                YearlyPrice = def.Yearly,
                CalculatedMonthlyPrice = def.CalcMonthly,
                CalculatedYearlyPrice = def.CalcYearly,
                IsPublic = true,
                IsCustom = false,
                Status = PackageStatus.Active,
                Tier = def.Tier,
                IsPopular = def.Popular,
                DisplayOrder = def.Order,
                TrialDays = 14,
                Features = packageFeatures
            });
        }
    }

}
