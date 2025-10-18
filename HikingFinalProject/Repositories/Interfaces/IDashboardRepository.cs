using HikingFinalProject.DTOs.Dashboard;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        /// <summary>
        /// Returns the full dashboard snapshot including counts, averages, and recent activity.
        /// </summary>
        Task<DashboardDto> GetDashboardAsync();
    }
}
