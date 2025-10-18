using HikingFinalProject.DTOs;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.MVC
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var dto = await _dashboardService.GetDashboardAsync();
            return View(dto); // Views/Dashboard/Index.cshtml
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About Dashboard";
            return View();
        }
    }
}
