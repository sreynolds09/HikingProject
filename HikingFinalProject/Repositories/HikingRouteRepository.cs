using Dapper;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.DTOs.Map;

namespace HikingFinalProject.Repositories
{
    public class HikingRouteRepository : IHikingRouteRepository
    {
        private readonly IDbConnection _conn;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _mapboxKey;

        public HikingRouteRepository(IDbConnection conn, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _conn = conn;
            _httpClientFactory = httpClientFactory;
            _mapboxKey = 
                
                config["Mapbox:ApiKey"];
        }

        // ==========================
        // GET ALL ROUTES + PARKS
        // ==========================
        public async Task<IEnumerable<HikingRoute>> GetAllRoutesAsync()
        {
            string sql = @"
                SELECT r.*, p.ParkID, p.ParkName, p.Location, p.Latitude AS ParkLat, p.Longitude AS ParkLng
                FROM Routes r
                LEFT JOIN Parks p ON r.ParkID = p.ParkID
                WHERE r.IsDeleted = 0";

            var routeDict = new Dictionary<int, HikingRoute>();

            var routes = await _conn.QueryAsync<HikingRoute, Park, HikingRoute>(
                sql,
                (route, park) =>
                {
                    if (!routeDict.TryGetValue(route.RouteID, out var currentRoute))
                    {
                        currentRoute = route;
                        routeDict.Add(currentRoute.RouteID, currentRoute);
                    }

                    if (park != null)
                    {
                        park.Latitude = park.Latitude ?? park.Latitude;
                        park.Longitude = park.Longitude ?? park.Longitude;
                        currentRoute.Park = park;
                    }

                    return currentRoute;
                },
                splitOn: "ParkID"
            );

            return routes.Distinct();
        }

        public async Task<HikingRoute?> GetRouteByIdAsync(int id)
        {
            string sql = @"
                SELECT r.*, p.ParkID, p.ParkName, p.Location, p.Latitude AS ParkLat, p.Longitude AS ParkLng
                FROM Routes r
                LEFT JOIN Parks p ON r.ParkID = p.ParkID
                WHERE r.RouteID = @Id AND r.IsDeleted = 0";

            var route = (await _conn.QueryAsync<HikingRoute, Park, HikingRoute>(
                sql,
                (r, p) =>
                {
                    r.Park = p;
                    return r;
                },
                new { Id = id },
                splitOn: "ParkID"
            )).FirstOrDefault();

            return route;
        }

        public async Task AddAsync(HikingRoute route)
        {
            string sql = @"INSERT INTO Routes 
                (RouteName, ParkID, Description, Difficulty, Latitude, Longitude, CreatedAt, UpdatedAt, IsDeleted)
                VALUES (@RouteName, @ParkID, @Description, @Difficulty, @Latitude, @Longitude, @CreatedAt, @UpdatedAt, 0)";
            await _conn.ExecuteAsync(sql, route);
        }

        public async Task UpdateAsync(HikingRoute route)
        {
            string sql = @"UPDATE Routes 
                SET RouteName=@RouteName, Description=@Description, Difficulty=@Difficulty, 
                    Latitude=@Latitude, Longitude=@Longitude, UpdatedAt=@UpdatedAt
                WHERE RouteID=@RouteID AND IsDeleted=0";
            await _conn.ExecuteAsync(sql, route);
        }

        public async Task SoftDeleteAsync(int id)
        {
            string sql = "UPDATE Routes SET IsDeleted=1 WHERE RouteID=@Id";
            await _conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<int> CountAsync()
        {
            string sql = "SELECT COUNT(*) FROM Routes WHERE IsDeleted=0";
            return await _conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<IEnumerable<RoutePoint>> GetRoutePointsAsync(int routeId)
        {
            string sql = "SELECT * FROM RoutePoints WHERE RouteID=@RouteID";
            return await _conn.QueryAsync<RoutePoint>(sql, new { RouteID = routeId });
        }

        public async Task<IEnumerable<RouteFeedback>> GetFeedbackForRouteAsync(int routeId)
        {
            string sql = "SELECT * FROM RouteFeedback WHERE RouteID=@RouteID ORDER BY CreatedAt DESC";
            return await _conn.QueryAsync<RouteFeedback>(sql, new { RouteID = routeId });
        }

        public async Task<IEnumerable<RouteImages>> GetImagesForRouteAsync(int routeId)
        {
            string sql = "SELECT * FROM RouteImages WHERE RouteID=@RouteID AND IsDeleted=0 ORDER BY CreatedAt DESC";
            return await _conn.QueryAsync<RouteImages>(sql, new { RouteID = routeId });
        }

        public async Task UpdateCoordinatesAsync(int routeId, double lat, double lng)
        {
            string sql = "UPDATE Routes SET Latitude=@Latitude, Longitude=@Longitude WHERE RouteID=@RouteID AND IsDeleted=0";
            await _conn.ExecuteAsync(sql, new { Latitude = lat, Longitude = lng, RouteID = routeId });
        }

        public async Task<int> GeocodeMissingRoutesAsync()
        {
            var routes = (await _conn.QueryAsync<HikingRoute>("SELECT * FROM Routes WHERE (Latitude IS NULL OR Longitude IS NULL) AND IsDeleted=0")).ToList();
            if (!routes.Any()) return 0;

            var client = _httpClientFactory.CreateClient();
            int updated = 0;

            foreach (var route in routes)
            {
                var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(route.RouteName)}.json?access_token={_mapboxKey}&limit=1";
                try
                {
                    var response = await client.GetFromJsonAsync<MapboxResponseDto>(url);
                    var feature = response?.Features?.FirstOrDefault();
                    if (feature?.Center?.Length == 2)
                    {
                        await UpdateCoordinatesAsync(route.RouteID, feature.Center[1], feature.Center[0]);
                        updated++;
                    }
                }
                catch { }
            }

            return updated;

        }


        public async Task AddRoutePointsAsync(IEnumerable<RoutePoint> points)
        {
            var sql = @"INSERT INTO RoutePoints (RouteID, Latitude, Longitude) VALUES (@RouteID, @Latitude, @Longitude)";
            await _conn.ExecuteAsync(sql, points);
        }

        public async Task<bool> UpdateGeoJsonAsync(int routeId, string geoJson)
        {
            string sql = "UPDATE Routes SET GeoJson = @geoJson WHERE RouteID = @routeId AND IsDeleted = 0";
            return await _conn.ExecuteAsync(sql, new { routeId, geoJson }) > 0;
        }

    }

}