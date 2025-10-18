using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Services
{
    public interface IRoutePointService
    {
        // Basic CRUD
        Task<IEnumerable<RoutePoint>> GetAllAsync();
        Task<RoutePoint?> GetByIdAsync(int id);
        Task<IEnumerable<RoutePoint>> GetByRouteIdAsync(int routeId);
        Task<int> AddAsync(RoutePointDto dto);
        Task<bool> UpdateAsync(RoutePointDto dto);
        Task<bool> SoftDeleteAsync(int id);

        Task<int> CountAsync();

        // Bulk operations
        Task AddBulkAsync(IEnumerable<RoutePointDto> dtos);
        Task UpdateBulkAsync(IEnumerable<RoutePointDto> dtos);
        Task DeleteBulkAsync(IEnumerable<int> ids);

        // Coordinates
        Task<bool> UpdateCoordinatesAsync(int id, double lat, double lng);
        Task<int> GeocodeMissingPointsAsync(string mapboxApiKey);

        IMapper Mapper { get; }
    }

    public class RoutePointService : IRoutePointService
    {
        private readonly IRoutePointRepository _repository;
        private readonly IMapper _mapper;

        public RoutePointService(IRoutePointRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public IMapper Mapper => _mapper;

        // =========================
        // Basic CRUD
        // =========================
        public async Task<IEnumerable<RoutePoint>> GetAllAsync() =>
            await _repository.GetAllAsync();

        public async Task<RoutePoint?> GetByIdAsync(int id) =>
            await _repository.GetByIdAsync(id);

        public async Task<IEnumerable<RoutePoint>> GetByRouteIdAsync(int routeId) =>
            await _repository.GetByRouteIdAsync(routeId);

        public async Task<int> AddAsync(RoutePointDto dto)
        {
            var point = _mapper.Map<RoutePoint>(dto);
            return await _repository.AddAsync(point);
        }

        public async Task<bool> UpdateAsync(RoutePointDto dto)
        {
            var point = _mapper.Map<RoutePoint>(dto);
            return await _repository.UpdateAsync(point);
        }

        public async Task<bool> SoftDeleteAsync(int id) =>
            await _repository.SoftDeleteAsync(id);

        public async Task<int> CountAsync() =>
            await _repository.CountAsync();

        // =========================
        // Bulk Operations
        // =========================
        public async Task AddBulkAsync(IEnumerable<RoutePointDto> dtos)
        {
            var points = dtos.Select(dto => _mapper.Map<RoutePoint>(dto));
            await _repository.AddBulkAsync(points);
        }

        public async Task UpdateBulkAsync(IEnumerable<RoutePointDto> dtos)
        {
            var points = dtos.Select(dto => _mapper.Map<RoutePoint>(dto));
            await _repository.UpdateBulkAsync(points);
        }

        public async Task DeleteBulkAsync(IEnumerable<int> ids) =>
            await _repository.DeleteBulkAsync(ids);

        // =========================
        // Coordinates
        // =========================
        public async Task<bool> UpdateCoordinatesAsync(int id, double lat, double lng) =>
            await _repository.UpdateCoordinatesAsync(id, lat, lng);

        public async Task<int> GeocodeMissingPointsAsync(string mapboxApiKey) =>
            await _repository.GeocodeMissingPointsAsync(mapboxApiKey);
    }
}
