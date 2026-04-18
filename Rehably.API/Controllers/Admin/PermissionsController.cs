using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Auth;
using Rehably.Application.DTOs.Role;
using Rehably.Application.DTOs.Admin;
using Rehably.Application.Common;
using Rehably.Domain.Entities.Identity;
using Rehably.Domain.Constants;
using System.Security.Claims;
using System.Reflection;
using PermissionType = Rehably.API.Authorization.Permission;
using DomainPermission = Rehably.Domain.Entities.Identity.Permission;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Permission management for role-based access control.
/// </summary>
[ApiController]
[Route("api/admin/permissions")]
[Authorize]
[RequirePermission("platform.manage_features")]
[Produces("application/json")]
[Tags("Admin - Permissions")]
public class PermissionsController : BaseController
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        RoleManager<ApplicationRole> roleManager,
        ILogger<PermissionsController> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all available permissions (filtered by resource if specified)
    /// </summary>
    /// <param name="resource">Optional resource name to filter (e.g., "clinics", "patients")</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated list of permissions</returns>
    /// <response code="200">Returns permissions</response>
    /// <response code="500">Server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<PagedResult<PermissionDto>> GetPermissions(
        [FromQuery] string? resource = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allPermissions = GetAllDefinedPermissions();

            var filtered = string.IsNullOrWhiteSpace(resource)
                ? allPermissions
                : allPermissions.Where(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase)).ToList();

            var total = filtered.Count;
            var paginated = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Success(new PagedResult<PermissionDto>
            {
                Items = paginated,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permissions");
            return InternalError("Failed to retrieve permissions");
        }
    }

    /// <summary>
    /// Get platform-level permissions matrix for role creation/editing UI.
    /// Returns resources with their available actions (view, create, update, delete).
    /// </summary>
    /// <returns>Permission matrix with resources and actions (localized names)</returns>
    /// <response code="200">Returns permission matrix</response>
    /// <response code="500">Server error</response>
    [HttpGet("platform")]
    [RequirePermission("roles.view")]
    [ProducesResponseType(typeof(ApiResponse<PlatformPermissionMatrixResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<PlatformPermissionMatrixResponse> GetPlatformPermissions()
    {
        try
        {
            var response = new PlatformPermissionMatrixResponse
            {
                Resources = PlatformPermissions.Resources.Select(r => new PermissionResourceDto
                {
                    Resource = r.Key,
                    NameEn = r.NameEn,
                    NameAr = r.NameAr,
                    Actions = r.Actions.Select(a => new PermissionActionDto
                    {
                        Action = a.Key,
                        Permission = $"{r.Key}.{a.Key}",
                        NameEn = a.NameEn,
                        NameAr = a.NameAr
                    }).ToList()
                }).ToList()
            };

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve platform permissions");
            return InternalError("Failed to retrieve platform permissions");
        }
    }

    private List<PermissionDto> GetAllDefinedPermissions()
    {
        var permissions = new List<PermissionDto>();

        permissions.Add(new PermissionDto { Name = PermissionType.Clinics.View.ToString(), Resource = "clinics", Action = "view" });
        permissions.Add(new PermissionDto { Name = PermissionType.Clinics.Create.ToString(), Resource = "clinics", Action = "create" });
        permissions.Add(new PermissionDto { Name = PermissionType.Clinics.Update.ToString(), Resource = "clinics", Action = "update" });
        permissions.Add(new PermissionDto { Name = PermissionType.Clinics.Delete.ToString(), Resource = "clinics", Action = "delete" });

        permissions.Add(new PermissionDto { Name = PermissionType.Patients.View.ToString(), Resource = "patients", Action = "view" });
        permissions.Add(new PermissionDto { Name = PermissionType.Patients.Create.ToString(), Resource = "patients", Action = "create" });
        permissions.Add(new PermissionDto { Name = PermissionType.Patients.Update.ToString(), Resource = "patients", Action = "update" });
        permissions.Add(new PermissionDto { Name = PermissionType.Patients.Delete.ToString(), Resource = "patients", Action = "delete" });

        permissions.Add(new PermissionDto { Name = PermissionType.Appointments.View.ToString(), Resource = "appointments", Action = "view" });
        permissions.Add(new PermissionDto { Name = PermissionType.Appointments.Create.ToString(), Resource = "appointments", Action = "create" });
        permissions.Add(new PermissionDto { Name = PermissionType.Appointments.Update.ToString(), Resource = "appointments", Action = "update" });
        permissions.Add(new PermissionDto { Name = PermissionType.Appointments.Delete.ToString(), Resource = "appointments", Action = "delete" });

        permissions.Add(new PermissionDto { Name = "invoices.view", Resource = "invoices", Action = "view" });
        permissions.Add(new PermissionDto { Name = "invoices.create", Resource = "invoices", Action = "create" });
        permissions.Add(new PermissionDto { Name = "invoices.update", Resource = "invoices", Action = "update" });
        permissions.Add(new PermissionDto { Name = "invoices.delete", Resource = "invoices", Action = "delete" });

        permissions.Add(new PermissionDto { Name = "payments.view", Resource = "payments", Action = "view" });
        permissions.Add(new PermissionDto { Name = "payments.create", Resource = "payments", Action = "create" });
        permissions.Add(new PermissionDto { Name = "payments.refund", Resource = "payments", Action = "refund" });

        permissions.Add(new PermissionDto { Name = "reports.view", Resource = "reports", Action = "view" });
        permissions.Add(new PermissionDto { Name = "reports.export", Resource = "reports", Action = "export" });

        permissions.Add(new PermissionDto { Name = "users.view", Resource = "users", Action = "view" });
        permissions.Add(new PermissionDto { Name = "users.create", Resource = "users", Action = "create" });
        permissions.Add(new PermissionDto { Name = "users.update", Resource = "users", Action = "update" });
        permissions.Add(new PermissionDto { Name = "users.delete", Resource = "users", Action = "delete" });

        permissions.Add(new PermissionDto { Name = "roles.view", Resource = "roles", Action = "view" });
        permissions.Add(new PermissionDto { Name = "roles.create", Resource = "roles", Action = "create" });
        permissions.Add(new PermissionDto { Name = "roles.update", Resource = "roles", Action = "update" });
        permissions.Add(new PermissionDto { Name = "roles.delete", Resource = "roles", Action = "delete" });

        permissions.Add(new PermissionDto { Name = "settings.view", Resource = "settings", Action = "view" });
        permissions.Add(new PermissionDto { Name = "settings.update", Resource = "settings", Action = "update" });

        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ManageFeatures.ToString(), Resource = "platform", Action = "manage_features" });
        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ManagePackages.ToString(), Resource = "platform", Action = "manage_packages" });
        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ManageSubscriptions.ToString(), Resource = "platform", Action = "manage_subscriptions" });
        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ManageFeatureCategories.ToString(), Resource = "platform", Action = "manage_feature_categories" });
        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ViewUsageStats.ToString(), Resource = "platform", Action = "view_usage_stats" });
        permissions.Add(new PermissionDto { Name = PermissionType.Platform.ManageClinics.ToString(), Resource = "platform", Action = "manage_clinics" });
        permissions.Add(new PermissionDto { Name = "platform.*", Resource = "platform", Action = "*" });

        permissions.Add(new PermissionDto { Name = "users.*", Resource = "users", Action = "*" });
        permissions.Add(new PermissionDto { Name = "roles.*", Resource = "roles", Action = "*" });
        permissions.Add(new PermissionDto { Name = "settings.*", Resource = "settings", Action = "*" });

        permissions.Add(new PermissionDto { Name = "*.*", Resource = "*", Action = "*" });

        return permissions;
    }
}
