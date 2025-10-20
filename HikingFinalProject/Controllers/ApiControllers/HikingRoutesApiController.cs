using HikingFinalProject.DTOs.Dashboard;
using HikingFinalProject.DTOs.Map;
using HikingFinalProject.DTOs.Mapbox;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml.Linq;

namespace HikingFinalProject.Controllers.API
{
    [ApiController]
    [Route("api/routes")]
    [ApiExplorerSettings(GroupName = "Routes")]
    public class HikingRoutesApiController : ControllerBase
    {
        private readonly IHikingRouteService routeService;
        private readonly IRoutePointService pointService;

        public HikingRoutesApiController(IHikingRouteService routeService, IRoutePointService pointService)
        {
            this.routeService = routeService;
            this.pointService = pointService;
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<HikingRouteDto>> GetRoute(int id)
        {
            var route = await routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();
            return Ok(route);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HikingRouteDto>>> GetAllRoutes()
        {
            var routes = await routeService.GetAllRoutesAsync();
            return Ok(routes);
        }

        [HttpPost]
        public async Task<ActionResult<HikingRouteDto>> CreateRoute(HikingRouteDto dto)
        {
            await routeService.AddRouteAsync(dto);
            var route = await routeService.GetRouteByIdAsync(dto.RouteID);
            return CreatedAtAction(nameof(GetRoute), new { id = route?.RouteID }, route);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(int id, [FromBody] HikingRouteDto dto)
        {
            dto.RouteID = id;
            await routeService.UpdateRouteAsync(dto);
            var updated = await routeService.GetRouteByIdAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            await routeService.SoftDeleteRouteAsync(id);
            return NoContent();
        }

        [HttpGet("{id}/feedback")]
        public async Task<ActionResult<IEnumerable<RouteFeedbackDto>>> GetFeedback(int id)
        {
            var route = await routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();
            return Ok(route.RecentFeedback ?? new List<RouteFeedbackDto>());
        }

        [HttpGet("{id}/images")]
        public async Task<ActionResult<IEnumerable<RouteImageDto>>> GetImages(int id)
        {
            var route = await routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();
            return Ok(route.RecentImages ?? new List<RouteImageDto>());
        }

        [HttpPost("geocode-missing")]
        public async Task<IActionResult> GeocodeMissingRoutes()
        {
            var updatedCount = await routeService.GeocodeMissingRoutesAsync();
            return Ok(new { Updated = updatedCount });
        }

        // ==============================
        // Upload and Parse GPX
        // ==============================
        [HttpPost("{routeId}/upload-gpx")]
        [RequestSizeLimit(10_000_000)] // 10 MB limit
        public async Task<IActionResult> UploadGpx(int routeId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No GPX file uploaded.");

            var tempPath = Path.GetTempFileName();

            try
            {
                // Save to temp file
                await using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                // Load and parse XML
                var xml = await System.IO.File.ReadAllTextAsync(tempPath);
                var gpx = XDocument.Parse(xml);
                var ns = gpx.Root?.Name.Namespace ?? XNamespace.None;

                // Support <trkpt> (track points) and <rtept> (route points)
                var pts = gpx.Descendants(ns + "trkpt");
                if (!pts.Any())
                    pts = gpx.Descendants(ns + "rtept");

                if (!pts.Any())
                    return BadRequest("No valid GPX points found.");

                // Parse all points
                var points = pts.Select((pt, i) =>
                {
                    decimal lat = 0, lon = 0;
                    double ele = 0;
                    DateTime? time = null;

                    if (double.TryParse(pt.Attribute("lat")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var latVal))
                        lat = (decimal)latVal;
                    if (double.TryParse(pt.Attribute("lon")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lonVal))
                        lon = (decimal)lonVal;
                    if (double.TryParse(pt.Element(ns + "ele")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var eleVal))
                        ele = eleVal;
                    if (DateTime.TryParse(pt.Element(ns + "time")?.Value, out var timeVal))
                        time = timeVal;

                    return new RoutePointDto
                    {
                        RouteID = routeId,
                        latitude = lat,
                        longitude = lon,
                        elevation = (decimal?)ele,
                        time = time,
                        pointOrder = i + 1,
                        description = pt.Element(ns + "name")?.Value
                                      ?? pt.Element(ns + "desc")?.Value
                                      ?? $"Point {i + 1}"
                    };
                }).Where(p => p.latitude != 0 && p.longitude != 0).ToList();

                if (!points.Any())
                    return BadRequest("No valid coordinate data found in GPX file.");

                // Delete existing points for the route (to prevent duplicates)
                var existing = await pointService.GetByRouteIdAsync(routeId);
                if (existing.Any())
                {
                    await pointService.DeleteBulkAsync(existing.Select(x => x.Id));
                }

                // Bulk insert new points
                await pointService.AddBulkAsync(points);

                // Optional: build GeoJSON for mapping
                var geoJson = new
                {
                    type = "FeatureCollection",
                    features = new[]
                    {
                new {
                    type = "Feature",
                    geometry = new {
                        type = "LineString",
                        coordinates = points.Select(p => new [] {
                            (double)p.longitude!.Value,
                            (double)p.latitude!.Value,
                            (double?)p.elevation
                        }).ToArray()
                    },
                    properties = new { RouteId = routeId }
                }
            }
                };

                // Update the route’s GeoJSON field if supported
                await routeService.UpdateGeoJsonAsync(routeId, geoJson);

                return Ok(new
                {
                    Message = $"✅ GPX parsed and stored successfully for route {routeId}.",
                    PointsAdded = points.Count,
                    BoundingBox = new
                    {
                        MinLat = points.Min(p => p.latitude),
                        MaxLat = points.Max(p => p.latitude),
                        MinLon = points.Min(p => p.longitude),
                        MaxLon = points.Max(p => p.longitude)
                    },
                    FirstPoint = points.First(),
                    LastPoint = points.Last()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error processing GPX", Details = ex.Message });
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        [HttpGet("{routeId}/download-gpx")]
        public async Task<IActionResult> DownloadGpx(int routeId)
        {
            var route = await routeService.GetRouteByIdAsync(routeId);
            if (route == null)
                return NotFound($"Route with ID {routeId} not found.");

            var points = await pointService.GetByRouteIdAsync(routeId);
            if (points == null || !points.Any())
                return NotFound($"No route points found for route ID {routeId}.");

            // Build GPX XML document
            XNamespace ns = "http://www.topografix.com/GPX/1/1";
            var gpx = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(ns + "gpx",
                    new XAttribute("version", "1.1"),
                    new XAttribute("creator", "HikingFinalProject"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "gpx", "http://www.topografix.com/GPX/1/1"),
                    new XElement(ns + "metadata",
                        new XElement(ns + "name", route.RouteName ?? $"Route {routeId}"),
                        new XElement(ns + "time", DateTime.UtcNow.ToString("O"))
                    ),
                    new XElement(ns + "trk",
                        new XElement(ns + "name", route.RouteName ?? $"Route {routeId}"),
                        new XElement(ns + "trkseg",
                            from p in points
                            orderby p.PointOrder
                            select new XElement(ns + "trkpt",
                                new XAttribute("lat", Convert.ToString(p.Latitude, CultureInfo.InvariantCulture) ?? "0"),
                                new XAttribute("lon", Convert.ToString(p.Longitude, CultureInfo.InvariantCulture) ?? "0"),
                                p.Elevation.HasValue
                                    ? new XElement(ns + "ele", Convert.ToString(p.Elevation.Value, CultureInfo.InvariantCulture))
                                    : null,
                                p.Time.HasValue
                                    ? new XElement(ns + "time", p.Time.Value.ToUniversalTime().ToString("O"))
                                    : null,
                                !string.IsNullOrWhiteSpace(p.Description)
                                    ? new XElement(ns + "desc", p.Description)
                                    : null
                            )
                        )
                    )
                )
            );

            // Convert to memory stream for download
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(false), 1024, true))
            {
                gpx.Save(writer);
            }
            memoryStream.Position = 0;

            var fileName = $"{route.RouteName ?? $"route_{routeId}"}_{DateTime.UtcNow:yyyyMMdd_HHmm}.gpx";

            return File(memoryStream, "application/gpx+xml", fileName);
        }
    }
}