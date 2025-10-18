using HikingFinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IRouteImageRepository
    {
        Task<IEnumerable<RouteImages>> GetAllAsync();
        Task<RouteImages?> GetByIdAsync(int id);
        Task<IEnumerable<RouteImages>> GetByRouteIdAsync(int routeId);
        Task<int> AddAsync(RouteImages image);
        Task<bool> UpdateAsync(RouteImages image);
        Task<bool> SoftDeleteAsync(int id);
        Task<int> CountAsync();
        Task<IEnumerable<RouteImages>> GetRecentAsync(int count);
        Task<int> GeocodeMissingImagesAsync();
        Task<bool> UpdateCoordinatesAsync(int imageId, double lat, double lng);
        Task<IEnumerable<RouteImages>> GetByParkIdAsync(int parkId);
    }
}
