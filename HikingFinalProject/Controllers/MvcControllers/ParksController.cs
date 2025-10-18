using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.MVC
{
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ParksController : Controller
    {
        private readonly IParkService _parkService;
        private readonly IMapper _mapper;
        private readonly IMapboxGeocodingService _mapboxService;

        public ParksController(
            IParkService parkService,
            IMapper mapper,
            IMapboxGeocodingService mapboxService)
        {
            _parkService = parkService;
            _mapper = mapper;
            _mapboxService = mapboxService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var parks = await _parkService.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ParkDto>>(parks) ?? new List<ParkDto>();

            foreach (var park in dtos)
            {
                park.Routes = (await _parkService.GetRoutesByParkIdAsync(park.ParkID))?.ToList()
                              ?? new List<HikingRouteDto>();
            }

            return View(dtos);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var park = await _parkService.GetByIdAsync(id);
            if (park == null) return NotFound();

            return View(_mapper.Map<ParkDto>(park));
        }

        [HttpGet("Create")]
        public IActionResult Create() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParkDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            // Call Mapbox before saving
            var coords = await _mapboxService.GeocodeAsync(dto.ParkName ?? dto.Address);
            if (coords.HasValue)
            {
                dto.Latitude = (decimal)coords.Value.lat;
                dto.Longitude = (decimal)coords.Value.lng;
            }

            await _parkService.AddAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var park = await _parkService.GetByIdAsync(id);
            if (park == null) return NotFound();

            return View(_mapper.Map<ParkDto>(park));
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParkDto dto)
        {
            if (id != dto.ParkID) return BadRequest();
            if (!ModelState.IsValid) return View(dto);

            // Update coordinates if needed
            var coords = await _mapboxService.GeocodeAsync(dto.ParkName ?? dto.Address);
            if (coords.HasValue)
            {
                dto.Latitude = (decimal)coords.Value.lat;
                dto.Longitude = (decimal)coords.Value.lng;
            }

            var success = await _parkService.UpdateAsync(dto);
            if (!success) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var park = await _parkService.GetByIdAsync(id);
            if (park == null) return NotFound();

            return View(_mapper.Map<ParkDto>(park));
        }

        [HttpPost("DeleteConfirmed/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _parkService.SoftDeleteAsync(id);
            if (!success) return NotFound();

            return RedirectToAction(nameof(Index));
        }
    }
}


