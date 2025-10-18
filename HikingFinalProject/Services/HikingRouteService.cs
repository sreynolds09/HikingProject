using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using HikingFinalProject.Repositories.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;

namespace HikingFinalProject.Services
{
    public interface IHikingRouteService
    {
        Task<IEnumerable<HikingRouteDto>> GetAllRoutesAsync();
        Task<HikingRouteDto?> GetRouteByIdAsync(int routeId);
        Task<(List<RoutePointDto> Points, string GeoJson)> ParseAndAddGpxPointsAndGeoJsonAsync(int routeId, string filePath);
        Task<List<RoutePointDto>> ParseAndAddGpxPointsAsync(int routeId, string filePath);
        Task SoftDeleteRouteAsync(int routeId);
        Task<int> CountAsync();
        Task<int> AddRouteAsync(HikingRouteDto dto);
        Task<bool> UpdateRouteAsync(HikingRouteDto dto);
        Task<bool> UpdateCoordinatesAsync(int routeId, double lat, double lng);
        Task<int> GeocodeMissingRoutesAsync();
        Task UpdateGeoJsonAsync(int routeId, object geoJson);

        IMapper Mapper { get; }
    }

    public class HikingRouteService : IHikingRouteService
    {
        private readonly IDapperContext _dapperContext;
        private readonly IMapper _mapper;
        private readonly ILogger<HikingRouteService> _logger;
        private readonly IMapboxGeocodingService _geocoding;
        private readonly IHikingRouteRepository _repo;
        private readonly IParkRepository _parkRepo; // ✅ Added for Park lookup

        public HikingRouteService(
            IDapperContext dapperContext,
            IMapper mapper,
            ILogger<HikingRouteService> logger,
            IMapboxGeocodingService geocoding,
            IHikingRouteRepository repo,
            IParkRepository parkRepo // ✅ Injected
        )
        {
            _dapperContext = dapperContext;
            _mapper = mapper;
            _logger = logger;
            _geocoding = geocoding;
            _repo = repo;
            _parkRepo = parkRepo;
        }

        public IMapper Mapper => _mapper;

        public async Task<IEnumerable<HikingRouteDto>> GetAllRoutesAsync()
        {
            var routes = await _repo.GetAllRoutesAsync();
            return Mapper.Map<IEnumerable<HikingRouteDto>>(routes);
        }

        public async Task<HikingRouteDto?> GetRouteByIdAsync(int routeId)
        {
            var route = await _repo.GetRouteByIdAsync(routeId);
            if (route == null) return null;

            var dto = Mapper.Map<HikingRouteDto>(route);
            dto.RecentPoints = (await _repo.GetRoutePointsAsync(routeId)).Select(p => Mapper.Map<RoutePointDto>(p)).ToList();
            dto.RecentFeedback = (await _repo.GetFeedbackForRouteAsync(routeId))?.Take(3).Select(f => Mapper.Map<RouteFeedbackDto>(f));
            dto.RecentImages = (await _repo.GetImagesForRouteAsync(routeId))?.Take(3).Select(i => Mapper.Map<RouteImageDto>(i));
            return dto;
        }

        // ✅ UPDATED: Auto-fill ParkName and timestamps
        public async Task<int> AddRouteAsync(HikingRouteDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Look up park name from ParkID
            if (dto.ParkID > 0)
            {
                var park = await _parkRepo.GetByIdAsync(dto.ParkID);
                if (park != null)
                    dto.ParkName = park.ParkName;
                else
                    _logger.LogWarning("No park found for ParkID {ParkID}", dto.ParkID);
            }

            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;

            var route = Mapper.Map<HikingRoute>(dto);
            await _repo.AddAsync(route);
            return route.RouteID;
        }

        // ✅ UPDATED: Auto-update ParkName if ParkID changes
        public async Task<bool> UpdateRouteAsync(HikingRouteDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Refresh ParkName if needed
            if (dto.ParkID > 0)
            {
                var park = await _parkRepo.GetByIdAsync(dto.ParkID);
                if (park != null)
                    dto.ParkName = park.ParkName;
            }

            dto.UpdatedAt = DateTime.UtcNow;

            var route = Mapper.Map<HikingRoute>(dto);
            await _repo.UpdateAsync(route);
            return true;
        }

        public async Task SoftDeleteRouteAsync(int routeId)
        {
            await _repo.SoftDeleteAsync(routeId);
        }

        public async Task<int> CountAsync()
        {
            return await _repo.CountAsync();
        }

        public async Task<bool> UpdateCoordinatesAsync(int routeId, double lat, double lng)
        {
            await _repo.UpdateCoordinatesAsync(routeId, lat, lng);
            return true;
        }

        public async Task<int> GeocodeMissingRoutesAsync()
        {
            var routes = (await _repo.GetAllRoutesAsync()).Where(r => !r.Latitude.HasValue || !r.Longitude.HasValue).ToList();
            if (!routes.Any()) return 0;

            int updated = 0;
            foreach (var route in routes)
            {
                var coords = await _geocoding.GeocodeAsync(route.RouteName ?? $"Route {route.RouteID}");
                if (coords.HasValue)
                {
                    await UpdateCoordinatesAsync(route.RouteID, coords.Value.lat, coords.Value.lng);
                    updated++;
                }
            }

            _logger.LogInformation("Geocoded {Count} routes missing coordinates", updated);
            return updated;
        }

        public async Task<(List<RoutePointDto> Points, string GeoJson)> ParseAndAddGpxPointsAndGeoJsonAsync(int routeId, string filePath)
        {
            var route = await GetRouteByIdAsync(routeId);
            if (route == null)
            {
                var newRoute = new HikingRouteDto
                {
                    RouteName = Path.GetFileNameWithoutExtension(filePath),
                    Description = "Auto-created from GPX",
                    Difficulty = "Unknown",
                    ParkID = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await AddRouteAsync(newRoute);
                routeId = newRoute.RouteID;
            }

            var points = new List<RoutePoint>();
            var geoJsonCoordinates = new List<string>();

            var gpxDoc = new System.Xml.XmlDocument();
            gpxDoc.Load(filePath);

            var nsManager = new System.Xml.XmlNamespaceManager(gpxDoc.NameTable);
            nsManager.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

            var trackPoints = gpxDoc.SelectNodes("//gpx:trkpt", nsManager);

            if (trackPoints != null)
            {
                foreach (System.Xml.XmlNode? trkpt in trackPoints)
                {
                    if (trkpt?.Attributes == null) continue;

                    var latAttr = trkpt.Attributes["lat"]?.Value;
                    var lonAttr = trkpt.Attributes["lon"]?.Value;
                    if (!double.TryParse(latAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)) continue;
                    if (!double.TryParse(lonAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon)) continue;

                    points.Add(new RoutePoint
                    {
                        RouteID = routeId,
                        Latitude = (decimal?)lat,
                        Longitude = (decimal?)lon
                    });

                    geoJsonCoordinates.Add($"[{lon},{lat}]");
                }
            }

            if (points.Any())
                await _repo.AddRoutePointsAsync(points);

            var geoJson = $@"{{ ""type"": ""LineString"", ""coordinates"": [{string.Join(",", geoJsonCoordinates)}] }}";
            await _repo.UpdateGeoJsonAsync(routeId, geoJson);

            var pointDtos = points.Select(p => Mapper.Map<RoutePointDto>(p)).ToList();
            return (pointDtos, geoJson);
        }

        public async Task<List<RoutePointDto>> ParseAndAddGpxPointsAsync(int routeId, string filePath)
        {
            if (!File.Exists(filePath)) return new List<RoutePointDto>();

            var doc = XDocument.Load(filePath);
            XNamespace ns = "http://www.topografix.com/GPX/1/1";

            var points = doc.Descendants(ns + "trkpt")
                .Select(p =>
                {
                    var latStr = p.Attribute("lat")?.Value;
                    var lonStr = p.Attribute("lon")?.Value;

                    var lat = decimal.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpLat) ? tmpLat : (decimal?)null;
                    var lon = decimal.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpLon) ? tmpLon : (decimal?)null;

                    decimal? elevation = null;
                    if (decimal.TryParse(p.Element(ns + "ele")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var elev))
                        elevation = elev;

                    return new RoutePointDto
                    {
                        RouteID = routeId,
                        latitude = lat,
                        longitude = lon,
                        elevation = elevation
                    };
                })
                .Where(p => p.latitude.HasValue && p.longitude.HasValue)
                .ToList();

            if (points.Any())
            {
                var pointEntities = points.Select(p => Mapper.Map<RoutePoint>(p)).ToList();
                await _repo.AddRoutePointsAsync(pointEntities);
            }

            return points;
        }

        public async Task UpdateGeoJsonAsync(int routeId, object geoJson)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(geoJson);
            await _repo.UpdateGeoJsonAsync(routeId, json);
        }


    }
}
