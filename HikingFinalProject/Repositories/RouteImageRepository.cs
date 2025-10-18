using Dapper;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using HikingFinalProject.Services;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories
{
    public class RouteImageRepository : IRouteImageRepository
    {
        private readonly IDbConnection _conn;
        private readonly IMapboxGeocodingService _geocoder;

        public RouteImageRepository(IDbConnection conn, IMapboxGeocodingService geocoder)
        {
            _conn = conn;
            _geocoder = geocoder;
        }

        public async Task<IEnumerable<RouteImages>> GetAllAsync()
        {
            string sql = "SELECT * FROM routeimages WHERE IsDeleted = 0";
            return (await _conn.QueryAsync<RouteImages>(sql)).ToList();
        }

        public async Task<RouteImages?> GetByIdAsync(int id)
        {
            string sql = "SELECT * FROM routeimages WHERE Id = @Id AND IsDeleted = 0";
            return await _conn.QueryFirstOrDefaultAsync<RouteImages>(sql, new { Id = id });
        }

        public async Task<IEnumerable<RouteImages>> GetByRouteIdAsync(int routeId)
        {
            string sql = "SELECT * FROM routeimages WHERE RouteID = @RouteID AND IsDeleted = 0";
            return (await _conn.QueryAsync<RouteImages>(sql, new { RouteID = routeId })).ToList();
        }

        public async Task<int> AddAsync(RouteImages image)
        {
            string sql = @"
                INSERT INTO routeimages 
                (RouteID, ImageURL, Caption, DateStamp, FileName, FileData, CreatedAt, FilePath, IsDeleted, UpdatedAt)
                VALUES (@RouteID, @ImageURL, @Caption, @DateStamp, @FileName, @FileData, @CreatedAt, @FilePath, @IsDeleted, @UpdatedAt);
                SELECT LAST_INSERT_ID();";
            return await _conn.ExecuteScalarAsync<int>(sql, image);
        }

        public async Task<bool> UpdateAsync(RouteImages image)
        {
            string sql = @"
                UPDATE routeimages SET 
                    RouteID=@RouteID, ImageURL=@ImageURL, Caption=@Caption, DateStamp=@DateStamp,
                    FileName=@FileName, FileData=@FileData, FilePath=@FilePath, UpdatedAt=@UpdatedAt
                WHERE Id=@Id";
            int rows = await _conn.ExecuteAsync(sql, image);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            string sql = "UPDATE routeimages SET IsDeleted=1, UpdatedAt=NOW() WHERE Id=@Id";
            int rows = await _conn.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<int> CountAsync()
        {
            string sql = "SELECT COUNT(*) FROM routeimages WHERE IsDeleted = 0";
            return await _conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<IEnumerable<RouteImages>> GetRecentAsync(int count)
        {
            string sql = "SELECT * FROM routeimages WHERE IsDeleted = 0 ORDER BY CreatedAt DESC LIMIT @Count";
            return (await _conn.QueryAsync<RouteImages>(sql, new { Count = count })).ToList();
        }

        public async Task<bool> UpdateCoordinatesAsync(int id, double latitude, double longitude)
        {
            string sql = "UPDATE routeimages SET Latitude = @latitude, Longitude = @longitude, UpdatedAt = NOW() WHERE Id = @id";
            int rows = await _conn.ExecuteAsync(sql, new { id, latitude, longitude });
            return rows > 0;
        }

        public async Task<IEnumerable<RouteImages>> GetByParkIdAsync(int parkId)
        {
            string sql = @"
                SELECT ri.* 
                FROM routeimages ri
                INNER JOIN routes r ON ri.RouteID = r.Id
                WHERE r.ParkID = @ParkID AND ri.IsDeleted = 0";
            return (await _conn.QueryAsync<RouteImages>(sql, new { ParkID = parkId })).ToList();
        }

        // =========================
        // Geocode missing images
        // =========================
        public async Task<int> GeocodeMissingImagesAsync()
        {
            string sqlSelect = "SELECT * FROM routeimages WHERE (Latitude IS NULL OR Longitude IS NULL) AND IsDeleted = 0";
            var images = (await _conn.QueryAsync<RouteImages>(sqlSelect)).ToList();

            if (!images.Any()) return 0;

            int updatedCount = 0;

            foreach (var img in images)
            {
                var query = !string.IsNullOrWhiteSpace(img.Caption) ? img.Caption : img.FileName;
                var coords = await _geocoder.GeocodeAsync(query);

                if (coords.HasValue)
                {
                    await UpdateCoordinatesAsync(img.Id, coords.Value.lat, coords.Value.lng);
                    updatedCount++;
                }
            }

            return updatedCount;
        }
    }
}


