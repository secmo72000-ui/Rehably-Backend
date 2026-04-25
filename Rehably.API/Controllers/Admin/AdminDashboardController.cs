using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehably.Application.DTOs.Platform;
using Rehably.Application.Services.Platform;

namespace Rehably.API.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
[Produces("application/json")]
[Tags("Admin - Dashboard")]
public class AdminDashboardController : BaseController
{
    private readonly IAdminDashboardService _service;

    public AdminDashboardController(IAdminDashboardService service) => _service = service;

    /// <summary>
    /// Platform admin dashboard — clinic counts, revenue, recent subscriptions.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken ct = default)
        => FromResult(await _service.GetDashboardAsync(ct));
}
