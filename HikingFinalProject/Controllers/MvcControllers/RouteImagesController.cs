using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace HikingFinalProject.Controllers.MVC
{
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RouteImagesController : Controller
    {
        private readonly IRouteImageService _service;
        private readonly IWebHostEnvironment _env;

        public RouteImagesController(IRouteImageService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();
            return View(image);
        }

        [HttpGet("")]
        public async Task<IActionResult> Index() => View(await _service.GetAllAsync());

        [HttpPost("Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int routeId, IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return RedirectToAction("Details", "HikingRoutes", new { id = routeId });

            var fileUrl = await SaveFileAsync(imageFile);
            var dto = new RouteImageDto { routeId = routeId, imageURL = fileUrl };
            await _service.AddAsync(dto);

            return RedirectToAction("Details", "HikingRoutes", new { id = routeId });
        }

        [HttpGet("Create")]
        public IActionResult CreateView() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateView(RouteImageDto dto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(dto);
            if (imageFile != null && imageFile.Length > 0) dto.imageURL = await SaveFileAsync(imageFile);

            await _service.AddAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> EditView(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();
            return View(image);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditView(int id, RouteImageDto dto, IFormFile? imageFile)
        {
            if (id != dto.id) return BadRequest();
            if (!ModelState.IsValid) return View(dto);

            if (imageFile != null && imageFile.Length > 0)
                dto.imageURL = await SaveFileAsync(imageFile);

            await _service.UpdateAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> DeleteView(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();
            return View(image);
        }

        [HttpPost("DeleteConfirmed/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound();

            DeleteFileIfExists(image.imageURL);
            await _service.SoftDeleteAsync(id);
            return RedirectToAction(nameof(Index));
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


