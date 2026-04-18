using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Subscription;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Subscription management for clinic packages and billing.
/// </summary>
/// <remarks>
/// Manages subscriptions linked to clinics, including lifecycle operations (create, cancel, renew, upgrade) and usage tracking.
/// All endpoints require the "platform.manage_subscriptions" permission.
/// </remarks>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
[RequirePermission("platform.manage_subscriptions")]
[Produces("application/json")]
[Tags("Admin - Subscriptions")]
public class SubscriptionsController : BaseController
{
    private readonly IPlatformSubscriptionService _subscriptionService;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly ISubscriptionModificationService _modificationService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IPlatformSubscriptionService subscriptionService,
        ISubscriptionLifecycleService lifecycleService,
        ISubscriptionModificationService modificationService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _lifecycleService = lifecycleService;
        _modificationService = modificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <returns>Subscription details</returns>
    /// <response code="200">Returns the subscription</response>
    /// <response code="404">Subscription not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _subscriptionService.GetSubscriptionByIdAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Get subscription with full details including package and feature information
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <returns>Subscription with package and feature details</returns>
    /// <response code="200">Returns the subscription with details</response>
    /// <response code="404">Subscription not found</response>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDetailDto>> GetWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _subscriptionService.GetSubscriptionWithDetailsAsync(id);
        return FromResult(result);
    }

    /// <summary>
    /// Get all subscriptions with pagination and optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page, max 100 (default: 20)</param>
    /// <param name="status">Optional subscription status filter</param>
    /// <param name="clinicId">Optional clinic ID filter</param>
    /// <returns>Paged list of subscriptions</returns>
    /// <response code="200">Returns paginated subscriptions</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SubscriptionDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null,
        [FromQuery] Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest(new { error = "Page and pageSize must be positive numbers" });
        if (pageSize > 100)
            pageSize = 100;

        var result = await _subscriptionService.GetSubscriptionsPagedAsync(page, pageSize, status, clinicId);
        return FromResult(result);
    }

    /// <summary>
    /// Cancel an active subscription
    /// </summary>
    /// <param name="id">Subscription ID to cancel</param>
    /// <param name="request">Cancellation request with reason and effective date</param>
    /// <returns>Updated subscription with cancelled status</returns>
    /// <response code="200">Subscription cancelled successfully</response>
    /// <response code="400">Cannot cancel (invalid status, business rule violation, etc.)</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionDetailDto>> Cancel(Guid id, [FromBody] CancelSubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _lifecycleService.CancelSubscriptionAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Renew an expired or expiring subscription
    /// </summary>
    /// <param name="id">Subscription ID to renew</param>
    /// <param name="request">Renewal request with new end date and billing details</param>
    /// <returns>Updated subscription with renewed period</returns>
    /// <response code="200">Subscription renewed successfully</response>
    /// <response code="400">Cannot renew (invalid status, business rule violation, etc.)</response>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionDetailDto>> Renew(Guid id, [FromBody] RenewSubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _lifecycleService.RenewSubscriptionAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Upgrade a subscription to a higher tier or add features
    /// </summary>
    /// <param name="id">Subscription ID to upgrade</param>
    /// <param name="request">Upgrade request with new package or features and pricing adjustments</param>
    /// <returns>Updated subscription with upgraded package or features</returns>
    /// <response code="200">Subscription upgraded successfully</response>
    /// <response code="400">Cannot upgrade (invalid status, downgrade attempt, business rule violation, etc.)</response>
    [HttpPost("{id:guid}/upgrade")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionDetailDto>> Upgrade(Guid id, [FromBody] UpgradeSubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _modificationService.UpgradeSubscriptionAsync(id, request);
        return FromResult(result);
    }

    /// <summary>
    /// Reset usage counter for a specific feature in a subscription
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="featureId">Feature ID whose usage should be reset</param>
    /// <returns>Success message on completion</returns>
    /// <response code="200">Usage reset successfully</response>
    /// <response code="400">Cannot reset usage (invalid subscription or feature, business rule violation, etc.)</response>
    [HttpPost("{id:guid}/reset-usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetUsage(Guid id, [FromQuery] Guid featureId, CancellationToken cancellationToken = default)
    {
        var result = await _lifecycleService.ResetUsageAsync(id, featureId);
        return FromResult(result);
    }
}
