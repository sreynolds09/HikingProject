using HikingFinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IRouteFeedbackRepository
    {
        Task<IEnumerable<RouteFeedback>> GetAllAsync();
        Task<RouteFeedback?> GetByIdAsync(int id);
        Task<IEnumerable<RouteFeedback>> GetByRouteIdAsync(int routeId);
        Task<int> CreateAsync(RouteFeedback feedback);
        Task<bool> UpdateAsync(RouteFeedback feedback);
        Task<bool> SoftDeleteAsync(int id);
        Task<int> CountAsync();
        Task<IEnumerable<RouteFeedback>> GetRecentAsync(int count);
        Task<(double avgRating, double avgSkill, double avgStrenuousness)?> GetAggregatesAsync(int routeId);

    }
}