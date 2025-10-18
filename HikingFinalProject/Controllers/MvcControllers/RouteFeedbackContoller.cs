using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.MVC
{
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RouteFeedbackController : Controller
    {
        private readonly IRouteFeedbackService _service;

        public RouteFeedbackController(IRouteFeedbackService service)
        {
            _service = service;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index() => View(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var feedback = await _service.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return View(feedback);
        }

        [HttpGet("Create")]
        public IActionResult CreateView() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateView(RouteFeedbackDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _service.CreateAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> EditView(int id)
        {
            var feedback = await _service.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return View(_service.Mapper.Map<RouteFeedbackDto>(feedback));
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditView(int id, RouteFeedbackDto dto)
        {
            if (id != dto.Id) return BadRequest();
            if (!ModelState.IsValid) return View(dto);
            await _service.UpdateAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> DeleteView(int id)
        {
            var feedback = await _service.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return View(feedback);
        }

        [HttpPost("DeleteConfirmed/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.SoftDeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}


