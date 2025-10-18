using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.API
{
    [ApiController]
    [Route("api/routefeedback")]
    [ApiExplorerSettings(GroupName = "Feedback")]
    public class RouteFeedbackApiController : ControllerBase

    {
        private readonly IRouteFeedbackService _service;

        public RouteFeedbackApiController(IRouteFeedbackService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var feedback = await _service.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return Ok(feedback);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RouteFeedbackDto dto)
        {
            await _service.CreateAsync(dto);
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RouteFeedbackDto dto)
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
    }
}
