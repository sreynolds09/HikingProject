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
        [RequestSizeLimit(10_000_000)] // 10 MB
        public async Task<IActionResult> UploadGpx(int routeId, IFormFile file, [FromQuery] bool parseImmediately = true)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No GPX file uploaded.");

            var tempPath = Path.GetTempFileName();

            try
            {
                // Save to temp path
                await using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                if (!parseImmediately)
                {
                    return Ok(new { Message = "GPX uploaded successfully (not parsed)." });
                }

                // Parse the GPX file
                var xml = await System.IO.File.ReadAllTextAsync(tempPath);
                var gpx = XDocument.Parse(xml);
                var ns = gpx.Root?.Name.Namespace ?? XNamespace.None;

                var pts = gpx.Descendants(ns + "trkpt");
                if (!pts.Any())
                    pts = gpx.Descendants(ns + "rtept");

                var points = pts.Select((pt, i) => new RoutePointDto
                {
                    RouteID = routeId,
                    latitude = (decimal?)double.Parse(pt.Attribute("lat")?.Value ?? "0", CultureInfo.InvariantCulture),
                    longitude = (decimal?)double.Parse(pt.Attribute("lon")?.Value ?? "0", CultureInfo.InvariantCulture),
                    elevation = pt.Element(ns + "ele")?.Value is string eStr && double.TryParse(eStr, out var e) ? (decimal?)e : null,
                    pointOrder = i + 1,
                    description = pt.Element(ns + "name")?.Value
                                  ?? pt.Element(ns + "desc")?.Value
                                  ?? $"Point {i + 1}"
                }).ToList();

                if (!points.Any())
                    return BadRequest("No valid GPX points found.");

                await pointService.AddBulkAsync(points);

                // Optional: Convert to GeoJSON for the route
                var geoJson = new
                {
                    type = "FeatureCollection",
                    features = new[]
                    {
                        new {
                            type = "Feature",
                            geometry = new {
                                type = "LineString",
                                coordinates = points.Select(p => new [] { (double)p.longitude!.Value, (double)p.latitude!.Value }).ToArray()
                            },
                            properties = new { RouteId = routeId }
                        }
                    }
                };

                // Save GeoJSON back to route
                await routeService.UpdateGeoJsonAsync(routeId, geoJson);

                return Ok(new
                {
                    Message = $"GPX uploaded and parsed successfully for route {routeId}.",
                    PointsAdded = points.Count,
                    GeoJson = geoJson
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error processing GPX: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
    }
}