using AutoMapper;
using HikingFinalProject.DTOs.Routes;
using HikingFinalProject.Models;
using HikingFinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikingFinalProject.Controllers.API
{
    [ApiController]
    [Route("api/parks")]
    [ApiExplorerSettings(GroupName = "Parks")]
    public class ParksApiController : ControllerBase
    {
        private readonly IParkService _parkService;
        private readonly IMapper _mapper;
        private readonly IMapboxGeocodingService _mapboxService;

        public ParksApiController(
            IParkService parkService,
            IMapper mapper,
            IMapboxGeocodingService mapboxService)
        {
            _parkService = parkService;
            _mapper = mapper;
            _mapboxService = mapboxService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllParks()
        {
            var parks = await _parkService.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ParkDto>>(parks);
            return Ok(dtos ?? new List<ParkDto>());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPark(int id)
        {
            var park = await _parkService.GetByIdAsync(id);
            if (park == null) return NotFound();

            return Ok(_mapper.Map<ParkDto>(park));
        }

        [HttpPost]
        public async Task<IActionResult> CreatePark([FromBody] ParkDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1️⃣ Geocode using the provided address (from the DTO)
            if (!string.IsNullOrWhiteSpace(dto.Address))
            {
                var coords = await _mapboxService.GeocodeAsync(dto.Address);
                if (coords.HasValue)
                {
                    dto.Latitude = (decimal)coords.Value.lat;
                    dto.Longitude = (decimal)coords.Value.lng;
                }
            }

            // 2️⃣ Save park using your service
            var id = await _parkService.AddAsync(dto);

            // 3️⃣ Retrieve the saved park
            var park = await _parkService.GetByIdAsync(id);
            if (park == null)
                return NotFound("Park could not be retrieved after creation.");

            // 4️⃣ Map to DTO and return
            var result = _mapper.Map<ParkDto>(park);
            return CreatedAtAction(nameof(GetPark), new { id = result.ParkID }, result);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePark(int id, [FromBody] ParkDto dto)
        {
            if (id != dto.ParkID) return BadRequest();

            // Geocode on update
            var coords = await _mapboxService.GeocodeAsync(dto.ParkName ?? dto.Address);
            if (coords.HasValue)
            {
                dto.Latitude = (decimal)coords.Value.lat;
                dto.Longitude = (decimal)coords.Value.lng;
            }

            var success = await _parkService.UpdateAsync(dto);
            if (!success) return NotFound();

            var updated = await _parkService.GetByIdAsync(id);
            return Ok(_mapper.Map<ParkDto>(updated));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePark(int id)
        {
            var success = await _parkService.SoftDeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    


// ======================
// Trigger Mapbox Geocoding for missing parks
// ======================
[HttpPost("geocode-missing")]
        public async Task<IActionResult> GeocodeMissingParks()
        {
            var updatedCount = await _parkService.GeocodeMissingParksAsync();
            return Ok(new { Updated = updatedCount });
        }
    }
}



