using DeveloperNotes.DTOs;
using DeveloperNotes.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperNotes.Controllers
{
    [ApiController]
    [Route("api/geohash")]

    public class GeoHashController(GeohashService geohashService) : ControllerBase
    {
        private readonly GeohashService _geohashService = geohashService;

        [HttpPost("collectors")]
        public async Task<IActionResult> AddCollector([FromBody] CreateCollectorRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.AddCollector(request, ct);

                return Ok(new
                {
                    Message = "User added successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("targets")]
        public async Task<IActionResult> AddTarget([FromBody] CreateTargetRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.AddTarget(request, ct);

                return Ok(new
                {
                    Message = "Target added successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("targets/{targetId}/nearby-collectors")]
        public async Task<IActionResult> GetNearbyCollectors([FromBody] NearbyCollectorsRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.GetNearbyCollectors(request, ct);

                return Ok(new
                {
                    Message = "Nearby collectors fetched successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("targets/{targetId}/assign")]
        public async Task<IActionResult> AssignTarget(string targetId, [FromBody] AssignTargetRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.AssignTarget(targetId, request, ct);

                return Ok(new
                {
                    Message = "Target assigned successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("targets")]
        public async Task<IActionResult> GetTargets(CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.GetTargets(ct);

                return Ok(new
                {
                    Message = "Targets fetched successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("collectors")]
        public async Task<IActionResult> GetCollectors(CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.GetCollectors(ct);

                return Ok(new
                {
                    Message = "Targets fetched successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetGeohash")]
        public async Task<IActionResult> GetGeohash([FromBody] GeohashRequest request, CancellationToken ct)
        {
            try
            {
                var geohash = await _geohashService.GetGeohash(request);

                return Ok(new
                {
                    Geohash = geohash
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }

}
