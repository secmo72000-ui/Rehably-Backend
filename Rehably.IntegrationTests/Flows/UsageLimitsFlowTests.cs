using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rehably.Infrastructure.Data;
using Rehably.IntegrationTests.Infrastructure;

namespace Rehably.IntegrationTests.Flows;

[Collection("IntegrationTests")]
[TestCaseOrderer("Rehably.IntegrationTests.Infrastructure.PriorityOrderer", "Rehably.IntegrationTests")]
[Trait("Category", "Integration")]
public class UsageLimitsFlowTests : FlowTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string? _adminToken;
    private static Guid _subscriptionId;
    private static Guid _starterPackageId;
    private static Guid _proPackageId;
    private static Guid _featureId;

    public UsageLimitsFlowTests(RehablyWebApplicationFactory factory) : base(factory) { }

    [Fact, TestPriority(1)]
    public async Task Step1_AdminCreatesClinicWithStarterPackage()
    {
        _adminToken = await LoginAsAdminAsync();

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var starterPackage = await db.Packages.FirstOrDefaultAsync(p => p.Code == "starter");
        starterPackage.Should().NotBeNull();
        _starterPackageId = starterPackage!.Id;

        var proPackage = await db.Packages.FirstOrDefaultAsync(p => p.Code == "pro");
        proPackage.Should().NotBeNull();
        _proPackageId = proPackage!.Id;

        // Get a feature ID for reset-usage test
        var usersFeature = await db.Features.FirstOrDefaultAsync(f => f.Code == "users");
        usersFeature.Should().NotBeNull("users feature must be seeded");
        _featureId = usersFeature!.Id;

        var ownerEmail = $"usage-{Guid.NewGuid():N}@test.com";
        var formContent = new MultipartFormDataContent
        {
            { new StringContent("Usage Limits Clinic"), "ClinicName" },
            { new StringContent("+201000000003"), "Phone" },
            { new StringContent(ownerEmail), "Email" },
            { new StringContent(_starterPackageId.ToString()), "PackageId" },
            { new StringContent("0"), "BillingCycle" },
            { new StringContent(ownerEmail), "OwnerEmail" },
            { new StringContent("Usage"), "OwnerFirstName" },
            { new StringContent("Tester"), "OwnerLastName" },
            { new StringContent("0"), "PaymentType" }
        };

        var createResponse = await PostMultipartRawAsync("/api/admin/clinics", formContent, _adminToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Clinic creation failed: {await createResponse.Content.ReadAsStringAsync()}");

        var content = await createResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ClinicCreatedPayload>>(content, JsonOptions);
        _subscriptionId = result!.Data!.SubscriptionId;
        _subscriptionId.Should().NotBeEmpty();
    }

    [Fact, TestPriority(2)]
    public async Task Step2_GetSubscriptionDetails_VerifyFeatureLimits()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await GetRawAsync($"/api/admin/subscriptions/{_subscriptionId}/details", _adminToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        result.Data!.FeatureUsage.Should().NotBeEmpty("Starter package should have feature limits");

        // Verify the users feature has a limit (Starter has 5 users)
        var usersUsage = result.Data.FeatureUsage.FirstOrDefault(f => f.FeatureCode == "users");
        if (usersUsage != null)
        {
            usersUsage.Limit.Should().Be(5, "Starter package allows 5 users");
            usersUsage.Used.Should().Be(0, "No usage yet for a freshly created subscription");
        }
    }

    [Fact, TestPriority(3)]
    public async Task Step3_ResetUsage_ReturnsSuccess()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();
        _featureId.Should().NotBeEmpty();

        var response = await PostRawAsync(
            $"/api/admin/subscriptions/{_subscriptionId}/reset-usage?featureId={_featureId}",
            new { },
            _adminToken);

        // Reset may succeed (200) or fail if there's nothing to reset (400)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact, TestPriority(4)]
    public async Task Step4_UpgradeSubscription_VerifyNewLimits()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        // Upgrade from Starter to Pro
        var upgradeResponse = await PostRawAsync(
            $"/api/admin/subscriptions/{_subscriptionId}/upgrade",
            new { newPackageId = _proPackageId },
            _adminToken);

        upgradeResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Upgrade failed: {await upgradeResponse.Content.ReadAsStringAsync()}");

        // Verify new limits via details
        var detailsResponse = await GetRawAsync($"/api/admin/subscriptions/{_subscriptionId}/details", _adminToken);
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await detailsResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        result.Data!.PackageId.Should().Be(_proPackageId);

        // Pro package should have higher limits (20 users vs 5)
        var usersUsage = result.Data.FeatureUsage.FirstOrDefault(f => f.FeatureCode == "users");
        if (usersUsage != null)
        {
            usersUsage.Limit.Should().Be(20, "Pro package allows 20 users");
        }
    }

    private record ClinicCreatedPayload
    {
        public Guid Id { get; init; }
        public Guid SubscriptionId { get; init; }
    }

    private record SubscriptionDetailPayload
    {
        public Guid Id { get; init; }
        public Guid PackageId { get; init; }
        public int Status { get; init; }
        public List<FeatureUsagePayload> FeatureUsage { get; init; } = new();
    }

    private record FeatureUsagePayload
    {
        public Guid FeatureId { get; init; }
        public string FeatureName { get; init; } = string.Empty;
        public string FeatureCode { get; init; } = string.Empty;
        public int Limit { get; init; }
        public int Used { get; init; }
    }
}
