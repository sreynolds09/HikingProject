using Dapper;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikingFinalProject.DTOs.Routes;
//using HikingProject.DTOs.Parks;

namespace HikingFinalProject.Services
{
    public interface IParkService
    {
        Task<IEnumerable<Park>> GetAllAsync();
        Task<ParkDto?> GetByIdAsync(int parkId);

        Task<bool> SoftDeleteAsync(int parkId);
        Task<int> CountAsync();

        // DTO-based
        Task<int> AddAsync(ParkDto dto);
        Task<bool> UpdateAsync(ParkDto dto);
        Task AddBulkAsync(IEnumerable<ParkDto> dtos);
        Task UpdateBulkAsync(IEnumerable<ParkDto> dtos);
        Task DeleteBulkAsync(IEnumerable<int> parkIds);

        // Related Routes
        Task<IEnumerable<HikingRouteDto>> GetRoutesByParkIdAsync(int parkId);

        // Coordinates
        Task<bool> UpdateCoordinatesAsync(int parkId, double latitude, double longitude);
        Task<int> GeocodeMissingParksAsync(); // now uses injected Mapbox service

        IMapper Mapper { get; }
    }

    public class ParkService : IParkService
    {
        private readonly IDapperContext _dapperContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ParkService> _logger;
        private readonly IMapboxGeocodingService _geocoding;

        public ParkService(
            IDapperContext dapperContext,
            IMapper mapper,
            ILogger<ParkService> logger,
            IMapboxGeocodingService geocoding)
        {
            _dapperContext = dapperContext;
            _mapper = mapper;
            _logger = logger;
            _geocoding = geocoding;
        }

        public IMapper Mapper => _mapper;

        // ===== Domain =====
        public async Task<IEnumerable<Park>> GetAllAsync()
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = "SELECT * FROM Parks WHERE IsDeleted = 0";
            return await conn.QueryAsync<Park>(sql);
        }

        public async Task<ParkDto?> GetByIdAsync(int parkId)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = "SELECT * FROM Parks WHERE ParkID = @Id AND IsDeleted = 0";
            var park = await conn.QuerySingleOrDefaultAsync<Park>(sql, new { Id = parkId });
            if (park == null) return null;

            var dto = Mapper.Map<ParkDto>(park);
            dto.Routes = (await GetRoutesByParkIdAsync(dto.ParkID)).ToList();
            return dto;
        }

        public async Task<bool> SoftDeleteAsync(int parkId)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = "UPDATE Parks SET IsDeleted = 1, UpdatedAt = NOW() WHERE ParkID = @Id";
            return await conn.ExecuteAsync(sql, new { Id = parkId }) > 0;
        }

        public async Task<int> CountAsync()
        {
            using var conn = _dapperContext.CreateConnection();
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Parks WHERE IsDeleted = 0");
        }

        // ===== DTO-based =====
        public async Task<int> AddAsync(ParkDto dto)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = @"INSERT INTO Parks 
                        (ParkName, Location, Description, Latitude, Longitude, ImageURL, CreatedAt, UpdatedAt, IsDeleted)
                        VALUES (@ParkName, @Location, @Description, @Latitude, @Longitude, @ImageURL, NOW(), NOW(), 0);
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<bool> UpdateAsync(ParkDto dto)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = @"UPDATE Parks SET
                        ParkName = @ParkName,
                        Location = @Location,
                        Description = @Description,
                        Latitude = @Latitude,
                        Longitude = @Longitude,
                        ImageURL = @ImageURL,
                        UpdatedAt = NOW()
                        WHERE ParkID = @ParkID AND IsDeleted = 0";
            return await conn.ExecuteAsync(sql, dto) > 0;
        }

        public async Task AddBulkAsync(IEnumerable<ParkDto> dtos)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = @"INSERT INTO Parks 
                        (ParkName, Location, Description, Latitude, Longitude, ImageURL, CreatedAt, UpdatedAt, IsDeleted)
                        VALUES (@ParkName, @Location, @Description, @Latitude, @Longitude, @ImageURL, NOW(), NOW(), 0);";
            await conn.ExecuteAsync(sql, dtos);
        }

        public async Task UpdateBulkAsync(IEnumerable<ParkDto> dtos)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = @"UPDATE Parks SET
                        ParkName = @ParkName,
                        Location = @Location,
                        Description = @Description,
                        Latitude = @Latitude,
                        Longitude = @Longitude,
                        ImageURL = @ImageURL,
                        UpdatedAt = NOW()
                        WHERE ParkID = @ParkID AND IsDeleted = 0";
            await conn.ExecuteAsync(sql, dtos);
        }

        public async Task DeleteBulkAsync(IEnumerable<int> parkIds)
        {
            using var conn = _dapperContext.CreateConnection();
            await conn.ExecuteAsync("UPDATE Parks SET IsDeleted = 1, UpdatedAt = NOW() WHERE ParkID IN @Ids",
                                    new { Ids = parkIds });
        }

        // ===== Routes =====
        public async Task<IEnumerable<HikingRouteDto>> GetRoutesByParkIdAsync(int parkId)
        {
            using var conn = _dapperContext.CreateConnection();
            var routes = await conn.QueryAsync<HikingRoute>("SELECT * FROM Routes WHERE ParkID = @ParkID AND IsDeleted = 0",
                                                            new { ParkID = parkId });
            return Mapper.Map<IEnumerable<HikingRouteDto>>(routes);
        }

        // ===== Coordinates =====
        public async Task<bool> UpdateCoordinatesAsync(int parkId, double latitude, double longitude)
        {
            using var conn = _dapperContext.CreateConnection();
            var sql = "UPDATE Parks SET Latitude=@Latitude, Longitude=@Longitude, UpdatedAt=NOW() WHERE ParkID=@ParkID AND IsDeleted=0";
            return await conn.ExecuteAsync(sql, new { ParkID = parkId, Latitude = latitude, Longitude = longitude }) > 0;
        }

        // ===== Geocoding =====
        public async Task<int> GeocodeMissingParksAsync()
        {
            using var conn = _dapperContext.CreateConnection();
            var parks = (await conn.QueryAsync<Park>("SELECT * FROM Parks WHERE (Latitude IS NULL OR Longitude IS NULL) AND IsDeleted = 0")).ToList();

            int updated = 0;
            foreach (var park in parks)
            {
                var coords = await _geocoding.GeocodeAsync(park.ParkName ?? $"Park {park.ParkID}");
                if (coords != null)
                {
                    await UpdateCoordinatesAsync(park.ParkID, coords.Value.lat, coords.Value.lng);
                    updated++;
                }
            }

            _logger.LogInformation("Geocoded {Count} parks missing coordinates", updated);
            return updated;
        }
    }
}
