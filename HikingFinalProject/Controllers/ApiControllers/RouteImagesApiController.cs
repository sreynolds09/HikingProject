using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace HikingFinalProject.Controllers.API
{
    [ApiController]
    [Route("api/routeimages")]
    [ApiExplorerSettings(GroupName = "Images")]
    public class RouteImagesApiController : ControllerBase

    {
        private readonly IRouteImageService _service;
        private readonly IWebHostEnvironment _env;

        public RouteImagesApiController(IRouteImageService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();
            return Ok(image);
        }

        [HttpGet("park/{parkId}")]
        public async Task<IActionResult> GetByPark(int parkId) =>
            Ok(await _service.GetByParkIdAsync(parkId));

        [HttpGet("route/{routeId}")]
        public async Task<IActionResult> GetByRoute(int routeId) =>
            Ok(await _service.GetByRouteIdAsync(routeId));

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Create([FromForm] RouteImageDto dto, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
                dto.imageURL = await SaveFileAsync(imageFile);

            await _service.AddAsync(dto);
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RouteImageDto dto)
        {
            if (id != dto.id) return BadRequest();
            await _service.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();

            DeleteFileIfExists(image.imageURL);
            await _service.SoftDeleteAsync(id);
            return NoContent();
        }

        [HttpGet("count")]
        public async Task<IActionResult> Count() => Ok(await _service.CountAsync());

        [HttpGet("recent/{count}")]
        public async Task<IActionResult> Recent(int count) =>
            Ok(await _service.GetRecentAsync(count));

        // ==============================
        // New endpoint: geocode missing images
        // ==============================
        [HttpPost("geocode-missing")]
        public async Task<IActionResult> GeocodeMissing()
        {
            int updatedCount = await _service.GeocodeMissingImagesAsync();
            return Ok(new { message = "Geocoding complete", updatedCount });
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var savePath = Path.Combine(uploadsPath, fileName);

            await using var stream = new FileStream(savePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/" + fileName;
        }

        private void DeleteFileIfExists(string? fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var filePath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
    }
}

