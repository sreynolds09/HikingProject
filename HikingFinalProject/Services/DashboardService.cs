using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.Repositories.Interfaces;
using System.Threading.Tasks;

namespace HikingFinalProject.Services
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repo;

        public DashboardService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public Task<DashboardDto> GetDashboardAsync()
        {
            return _repo.GetDashboardAsync();
        }
    }
}
