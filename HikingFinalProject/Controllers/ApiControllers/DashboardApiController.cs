using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "Dashboard")]
public class DashboardApiController(DashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Returns the full dashboard data set (stats, recent items, map data).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var dto = await dashboardService.GetDashboardAsync();
        if (dto == null)
            return NotFound("Dashboard data unavailable.");

        return Ok(dto);
    }

    /// <summary>
    /// Returns only key metrics for lightweight polling.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        var dto = await dashboardService.GetDashboardAsync();
        if (dto == null)
            return NotFound();

        return Ok(new
        {
            dto.TotalParks,
            dto.TotalRoutes,
            dto.TotalFeedback,
            dto.TotalImages,
            dto.PointCount,
            dto.AverageRouteRating
        });
    }
}