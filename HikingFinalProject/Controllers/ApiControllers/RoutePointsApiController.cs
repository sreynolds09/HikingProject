using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HikingFinalProject.Controllers.API
{
    [ApiController]
    [Route("api/routepoints")]
    [ApiExplorerSettings(GroupName = "Points")]
    public class RoutePointsApiController : ControllerBase

    {
        private readonly IRoutePointService _service;

        public RoutePointsApiController(IRoutePointService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var point = await _service.GetByIdAsync(id);
            if (point == null) return NotFound();
            return Ok(point);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoutePointDto dto)
        {
            await _service.AddAsync(dto);
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoutePointDto dto)
        {
            dto.Id = id;
            await _service.UpdateAsync(dto);
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.SoftDeleteAsync(id);
            return NoContent();
        }

        [HttpPost("upload-gpx/{routeId}")]
        [RequestSizeLimit(10_000_000)] // 10 MB limit
        public async Task<IActionResult> UploadGpx(int routeId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No GPX file provided.");

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var xml = await reader.ReadToEndAsync();

                var gpx = XDocument.Parse(xml);
                var ns = gpx.Root?.Name.Namespace ?? XNamespace.None;

                // Support both track points and route points
                var pts = gpx.Descendants(ns + "trkpt");
                if (!pts.Any())
                    pts = gpx.Descendants(ns + "rtept");

                var points = pts.Select((pt, i) => new RoutePointDto
                {
                    RouteID = routeId,
                    latitude = (decimal?)double.Parse(pt.Attribute("lat")?.Value ?? "0", CultureInfo.InvariantCulture),
                    longitude = (decimal?)double.Parse(pt.Attribute("lon")?.Value ?? "0", CultureInfo.InvariantCulture),
                    pointOrder = i + 1,
                    description = pt.Element(ns + "name")?.Value
                                  ?? pt.Element(ns + "desc")?.Value
                                  ?? $"Point {i + 1}"
                }).ToList();

                if (!points.Any())
                    return BadRequest("No valid GPX points found.");

                await _service.AddBulkAsync(points);

                // Re-fetch from database for confirmation / mapping
                var savedPoints = await _service.GetByRouteIdAsync(routeId);

                return Ok(new
                {
                    message = $"Uploaded {points.Count} points for route {routeId}.",
                    count = points.Count,
                    points = savedPoints
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest($"GPX upload failed: {ex.Message}");
            }
        }
    }
}