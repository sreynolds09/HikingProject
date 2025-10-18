using Dapper;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HikingFinalProject.Repositories
{
    public class RoutePointRepository : IRoutePointRepository
    {
        private readonly IDbConnection _conn;
        private readonly IHttpClientFactory? _httpClientFactory;

        public RoutePointRepository(IDbConnection conn, IHttpClientFactory? httpClientFactory = null)
        {
            _conn = conn;
            _httpClientFactory = httpClientFactory;
        }

        // =========================
        // Basic CRUD
        // =========================
        public async Task<IEnumerable<RoutePoint>> GetAllAsync()
        {
            string sql = "SELECT * FROM RoutePoints WHERE IsDeleted=0 ORDER BY CreatedAt DESC";
            return (await _conn.QueryAsync<RoutePoint>(sql)).ToList();
        }

        public async Task<IEnumerable<RoutePoint>> GetByRouteIdAsync(int routeId)
        {
            string sql = "SELECT * FROM RoutePoints WHERE RouteId=@RouteId AND IsDeleted=0 ORDER BY PointOrder ASC";
            return (await _conn.QueryAsync<RoutePoint>(sql, new { RouteId = routeId })).ToList();
        }

        public async Task<RoutePoint?> GetByIdAsync(int pointId)
        {
            string sql = "SELECT * FROM RoutePoints WHERE Id=@Id AND IsDeleted=0";
            return await _conn.QueryFirstOrDefaultAsync<RoutePoint>(sql, new { Id = pointId });
        }

        public async Task<int> AddAsync(RoutePoint point)
        {
            string sql = @"INSERT INTO RoutePoints (RouteId, Latitude, Longitude, PointOrder, Description, CreatedAt, IsDeleted)
                           VALUES (@RouteId, @Latitude, @Longitude, @PointOrder, @Description, @CreatedAt, 0);
                           SELECT LAST_INSERT_ID();";
            return await _conn.ExecuteScalarAsync<int>(sql, point);
        }

        public async Task<bool> UpdateAsync(RoutePoint point)
        {
            string sql = @"UPDATE RoutePoints
                           SET Latitude=@Latitude, Longitude=@Longitude, PointOrder=@PointOrder, Description=@Description
                           WHERE Id=@Id AND IsDeleted=0";
            int rows = await _conn.ExecuteAsync(sql, point);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int pointId)
        {
            string sql = "UPDATE RoutePoints SET IsDeleted=1 WHERE Id=@Id";
            int rows = await _conn.ExecuteAsync(sql, new { Id = pointId });
            return rows > 0;
        }

        public async Task<int> CountAsync()
        {
            string sql = "SELECT COUNT(*) FROM RoutePoints WHERE IsDeleted=0";
            return await _conn.ExecuteScalarAsync<int>(sql);
        }

        // =========================
        // Bulk Operations
        // =========================
        public async Task AddBulkAsync(IEnumerable<RoutePoint> points)
        {
            string sql = @"INSERT INTO RoutePoints (RouteId, Latitude, Longitude, PointOrder, Description, CreatedAt, IsDeleted)
                           VALUES (@RouteId, @Latitude, @Longitude, @PointOrder, @Description, @CreatedAt, 0)";
            await _conn.ExecuteAsync(sql, points);
        }

        public async Task UpdateBulkAsync(IEnumerable<RoutePoint> points)
        {
            string sql = @"UPDATE RoutePoints
                           SET Latitude=@Latitude, Longitude=@Longitude, PointOrder=@PointOrder, Description=@Description
                           WHERE Id=@Id AND IsDeleted=0";
            await _conn.ExecuteAsync(sql, points);
        }

        public async Task DeleteBulkAsync(IEnumerable<int> pointIds)
        {
            string sql = "UPDATE RoutePoints SET IsDeleted=1 WHERE Id=@Id";
            await _conn.ExecuteAsync(sql, pointIds.Select(id => new { Id = id }));
        }

        // =========================
        // Coordinates
        // =========================
        public async Task<bool> UpdateCoordinatesAsync(int id, double latitude, double longitude)
        {
            string sql = "UPDATE RoutePoints SET Latitude=@latitude, Longitude=@longitude WHERE Id=@id";
            int rows = await _conn.ExecuteAsync(sql, new { id, latitude, longitude });
            return rows > 0;
        }

        public async Task<int> GeocodeMissingPointsAsync(string apiKey)
        {
            if (_httpClientFactory == null) return 0;

            string sqlSelect = "SELECT * FROM RoutePoints WHERE (Latitude IS NULL OR Longitude IS NULL) AND IsDeleted=0";
            var points = (await _conn.QueryAsync<RoutePoint>(sqlSelect)).ToList();
            if (!points.Any()) return 0;

            var client = _httpClientFactory.CreateClient();
            int updatedCount = 0;

            foreach (var point in points)
            {
                string query = string.IsNullOrWhiteSpace(point.Description)
                    ? $"Route Point {point.Id}"
                    : point.Description;

                string url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{System.Uri.EscapeDataString(query)}.json?access_token={apiKey}&limit=1";

                try
                {
                    var response = await client.GetFromJsonAsync<MapboxResponse>(url);
                    var feature = response?.Features?.FirstOrDefault();

                    if (feature?.Geometry?.Coordinates?.Length == 2)
                    {
                        double lon = feature.Geometry.Coordinates[0];
                        double lat = feature.Geometry.Coordinates[1];
                        await UpdateCoordinatesAsync(point.Id, lat, lon);
                        updatedCount++;
                    }
                }
                catch
                {
                    // ignore errors
                }
            }

            return updatedCount;
        }

        // =========================
        // Mapbox DTOs
        // =========================
        private class MapboxResponse
        {
            public List<MapboxFeature>? Features { get; set; }
        }

        private class MapboxFeature
        {
            public MapboxGeometry Geometry { get; set; } = new();
        }

        private class MapboxGeometry
        {
            public double[] Coordinates { get; set; } = new double[0];
        }
    }
}
