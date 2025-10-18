using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using HikingFinalProject.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Services
{
    public interface IRouteImageService
    {
        Task<IEnumerable<RouteImageDto>> GetAllAsync();
        Task<RouteImageDto?> GetByIdAsync(int id);
        Task<IEnumerable<RouteImageDto>> GetByRouteIdAsync(int routeId);
        Task<IEnumerable<RouteImageDto>> GetByParkIdAsync(int parkId);
        Task<IEnumerable<RouteImageDto>> GetRecentAsync(int count);

        Task AddAsync(RouteImageDto dto);
        Task UpdateAsync(RouteImageDto dto);
        Task SoftDeleteAsync(int id);

        Task<int> CountAsync();
        Task<int> GeocodeMissingImagesAsync();
        Task<bool> UpdateCoordinatesAsync(int imageId, double lat, double lng);
    }

    public class RouteImageService : IRouteImageService
    {
        private readonly IRouteImageRepository _repo;
        private readonly IMapper _mapper;
        private readonly IMapboxGeocodingService _geocoder;

        public RouteImageService(IRouteImageRepository repo, IMapper mapper, IMapboxGeocodingService geocoder)
        {
            _repo = repo;
            _mapper = mapper;
            _geocoder = geocoder;
        }

        public async Task<IEnumerable<RouteImageDto>> GetAllAsync() =>
            _mapper.Map<IEnumerable<RouteImageDto>>(await _repo.GetAllAsync());

        public async Task<RouteImageDto?> GetByIdAsync(int id) =>
            _mapper.Map<RouteImageDto>(await _repo.GetByIdAsync(id));

        public async Task<IEnumerable<RouteImageDto>> GetByRouteIdAsync(int routeId) =>
            _mapper.Map<IEnumerable<RouteImageDto>>(await _repo.GetByRouteIdAsync(routeId));

        public async Task<IEnumerable<RouteImageDto>> GetByParkIdAsync(int parkId) =>
            _mapper.Map<IEnumerable<RouteImageDto>>(await _repo.GetByParkIdAsync(parkId));

        public async Task<IEnumerable<RouteImageDto>> GetRecentAsync(int count) =>
            _mapper.Map<IEnumerable<RouteImageDto>>(await _repo.GetRecentAsync(count));

        public async Task AddAsync(RouteImageDto dto) =>
            await _repo.AddAsync(_mapper.Map<RouteImages>(dto));

        public async Task UpdateAsync(RouteImageDto dto) =>
            await _repo.UpdateAsync(_mapper.Map<RouteImages>(dto));

        public async Task SoftDeleteAsync(int id) =>
            await _repo.SoftDeleteAsync(id);

        public async Task<int> CountAsync() => await _repo.CountAsync();

        /// <summary>
        /// Geocodes all route images missing Latitude/Longitude using the injected IMapboxGeocodingService.
        /// Updates the repository with coordinates.
        /// </summary>
        public async Task<int> GeocodeMissingImagesAsync()
        {
            var images = await _repo.GetAllAsync();
            var missing = images.Where(i => i.Latitude == null || i.Longitude == null).ToList();
            int updated = 0;

            foreach (var img in missing)
            {
                // Prefer caption over filename as geocoding query
                var query = !string.IsNullOrWhiteSpace(img.Caption) ? img.Caption : img.FileName;
                var coords = await _geocoder.GeocodeAsync(query);

                if (coords.HasValue)
                {
                    await _repo.UpdateCoordinatesAsync(img.Id, coords.Value.lat, coords.Value.lng);
                    updated++;
                }
            }

            return updated;
        }

        public Task<bool> UpdateCoordinatesAsync(int id, double latitude, double longitude) =>
            _repo.UpdateCoordinatesAsync(id, latitude, longitude);
    }
}
