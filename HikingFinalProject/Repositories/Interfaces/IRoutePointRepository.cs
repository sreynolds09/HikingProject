using HikingFinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IRoutePointRepository
    {
        // Basic CRUD
        Task<IEnumerable<RoutePoint>> GetAllAsync();
        Task<IEnumerable<RoutePoint>> GetByRouteIdAsync(int routeId);
        Task<RoutePoint?> GetByIdAsync(int id);

        Task<int> AddAsync(RoutePoint point);
        Task<bool> UpdateAsync(RoutePoint point);
        Task<bool> SoftDeleteAsync(int id);

        Task<int> CountAsync();

        // Bulk operations
        Task AddBulkAsync(IEnumerable<RoutePoint> points);
        Task UpdateBulkAsync(IEnumerable<RoutePoint> points);
        Task DeleteBulkAsync(IEnumerable<int> pointIds);

        // Coordinates
        Task<bool> UpdateCoordinatesAsync(int id, double latitude, double longitude);
        Task<int> GeocodeMissingPointsAsync(string apiKey);
    }
}

