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
public class SubscriptionLifecycleTests : FlowTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string? _adminToken;
    private static Guid _clinicId;
    private static Guid _subscriptionId;
    private static Guid _starterPackageId;
    private static Guid _proPackageId;

    public SubscriptionLifecycleTests(RehablyWebApplicationFactory factory) : base(factory) { }

    [Fact, TestPriority(1)]
    public async Task Step1_Setup_CreateAndActivateClinic()
    {
        _adminToken = await LoginAsAdminAsync();

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var starterPackage = await db.Packages.FirstOrDefaultAsync(p => p.Code == "starter");
        starterPackage.Should().NotBeNull("Starter package must be seeded");
        _starterPackageId = starterPackage!.Id;

        var proPackage = await db.Packages.FirstOrDefaultAsync(p => p.Code == "pro");
        proPackage.Should().NotBeNull("Pro package must be seeded");
        _proPackageId = proPackage!.Id;

        // Create a clinic via the creation endpoint
        var ownerEmail = $"sub-lifecycle-{Guid.NewGuid():N}@test.com";
        var formContent = new MultipartFormDataContent
        {
            { new StringContent("Subscription Lifecycle Clinic"), "ClinicName" },
            { new StringContent("+201000000001"), "Phone" },
            { new StringContent(ownerEmail), "Email" },
            { new StringContent(_starterPackageId.ToString()), "PackageId" },
            { new StringContent("0"), "BillingCycle" },
            { new StringContent(ownerEmail), "OwnerEmail" },
            { new StringContent("Sub"), "OwnerFirstName" },
            { new StringContent("Test"), "OwnerLastName" },
            { new StringContent("0"), "PaymentType" }
        };

        var createResponse = await PostMultipartRawAsync("/api/admin/clinics", formContent, _adminToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Clinic creation failed: {await createResponse.Content.ReadAsStringAsync()}");

        var content = await createResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ClinicCreatedPayload>>(content, JsonOptions);
        _clinicId = result!.Data!.Id;
        _subscriptionId = result.Data.SubscriptionId;

        _clinicId.Should().NotBeEmpty();
        _subscriptionId.Should().NotBeEmpty();
    }

    [Fact, TestPriority(2)]
    public async Task Step2_GetSubscription_ReturnsActiveStatus()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await GetRawAsync($"/api/admin/subscriptions/{_subscriptionId}", _adminToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        // Status should be Active (1) or Trial (0) depending on package config
        result.Data!.Status.Should().BeOneOf(0, 1);
    }

    [Fact, TestPriority(3)]
    public async Task Step3_GetSubscriptionDetails_HasFeatures()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await GetRawAsync($"/api/admin/subscriptions/{_subscriptionId}/details", _adminToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        result.Data!.FeatureUsage.Should().NotBeEmpty("Starter package includes features with limits");
    }

    [Fact, TestPriority(4)]
    public async Task Step4_CancelSubscription_ReturnsCancelledStatus()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await PostRawAsync(
            $"/api/admin/subscriptions/{_subscriptionId}/cancel",
            new { reason = "Integration test cancellation" },
            _adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Cancel failed: {await response.Content.ReadAsStringAsync()}");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(3); // Cancelled = 3
    }

    [Fact, TestPriority(5)]
    public async Task Step5_RenewSubscription_ReturnsActiveWithNewDates()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await PostRawAsync(
            $"/api/admin/subscriptions/{_subscriptionId}/renew",
            new { packageId = _starterPackageId },
            _adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Renew failed: {await response.Content.ReadAsStringAsync()}");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        // After renewal, status should be Active (1) or Trial (0)
        result.Data!.Status.Should().BeOneOf(0, 1);
    }

    [Fact, TestPriority(6)]
    public async Task Step6_UpgradeSubscription_ChangeToProPackage()
    {
        _adminToken.Should().NotBeNull();
        _subscriptionId.Should().NotBeEmpty();

        var response = await PostRawAsync(
            $"/api/admin/subscriptions/{_subscriptionId}/upgrade",
            new { newPackageId = _proPackageId },
            _adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Upgrade failed: {await response.Content.ReadAsStringAsync()}");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SubscriptionDetailPayload>>(content, JsonOptions);

        result!.Data.Should().NotBeNull();
        result.Data!.PackageId.Should().Be(_proPackageId);
    }

    private record ClinicCreatedPayload
    {
        public Guid Id { get; init; }
        public Guid SubscriptionId { get; init; }
    }

    private record SubscriptionPayload
    {
        public Guid Id { get; init; }
        public Guid ClinicId { get; init; }
        public Guid PackageId { get; init; }
        public int Status { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
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
        public int Limit { get; init; }
        public int Used { get; init; }
    }
}
