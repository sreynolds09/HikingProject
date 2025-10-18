using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.MVC
{
    [Route("Routes")] // Add this to the controller
   // [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HikingRoutesController : Controller
    {
        private readonly IHikingRouteService _routeService;
        private readonly IParkService _parkService;
        private readonly IMapper _mapper;

        public HikingRoutesController(IHikingRouteService routeService, IParkService parkService, IMapper mapper)
        {
            _routeService = routeService;
            _parkService = parkService;
            _mapper = mapper;
        }

        // ===========================
        // GET: /HikingRoutes
        // ===========================
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var routes = await _routeService.GetAllRoutesAsync();
            return View(routes); // ParkDto and other properties already populated
        }

        // ===========================
        // GET: /HikingRoutes/Details/5
        // ===========================
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var route = await _routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();

            return View(route);
        }

        // ===========================
        // GET: /HikingRoutes/Create
        // ===========================
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Parks = await _parkService.GetAllAsync();
            return View(new HikingRouteDto());
        }

        // ===========================
        // POST: /HikingRoutes/Create
        // ===========================
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HikingRouteDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Parks = await _parkService.GetAllAsync();
                return View(dto);
            }

            await _routeService.AddRouteAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // GET: /HikingRoutes/Edit/5
        // ===========================
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var route = await _routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();

            ViewBag.Parks = await _parkService.GetAllAsync();
            return View(route);
        }

        // ===========================
        // POST: /HikingRoutes/Edit/5
        // ===========================
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HikingRouteDto dto)
        {
            if (id != dto.RouteID) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Parks = await _parkService.GetAllAsync();
                return View(dto);
            }

            await _routeService.UpdateRouteAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // GET: /HikingRoutes/Delete/5
        // ===========================
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var route = await _routeService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();

            return View(route);
        }

        // ===========================
        // POST: /HikingRoutes/DeleteConfirmed/5
        // ===========================
        [HttpPost("DeleteConfirmed/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _routeService.SoftDeleteRouteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

