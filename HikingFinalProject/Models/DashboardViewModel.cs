using HikingFinalProject.DTOs.Dashboard;

namespace HikingFinalroject.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardDto Summary { get; set; } = new();
        public DashboardStatsDto Stats { get; set; } = new();
    }
}

