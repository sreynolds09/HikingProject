using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories.Interfaces
{
    public interface IHikingRouteRepository
    {
        Task<IEnumerable<HikingRoute>> GetAllRoutesAsync();
        Task<bool> UpdateGeoJsonAsync(int routeId, string geoJson);
        Task AddRoutePointsAsync(IEnumerable<RoutePoint> points);

        Task<HikingRoute?> GetRouteByIdAsync(int id);
        Task AddAsync(HikingRoute route);
        Task UpdateAsync(HikingRoute route);
        Task SoftDeleteAsync(int id);
        Task<int> CountAsync();
        Task<IEnumerable<RoutePoint>> GetRoutePointsAsync(int routeId);
        Task<IEnumerable<RouteFeedback>> GetFeedbackForRouteAsync(int routeId);
        Task<IEnumerable<RouteImages>> GetImagesForRouteAsync(int routeId);
        Task UpdateCoordinatesAsync(int routeId, double lat, double lng);
        Task<int> GeocodeMissingRoutesAsync();
    }
}

