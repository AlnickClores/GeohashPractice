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

        [HttpPost("AddUserAndLocation")]
        public async Task<IActionResult> AddUserAndLocation([FromBody] AddUserAndLocationRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.AddUserAndLocation(request, ct);

                return Ok(new
                {
                    Message = "User and location added successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("AddTargetAndLocation")]
        public async Task<IActionResult> AddTargetAndLocation([FromBody] AddUserAndLocationRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.AddTargetAndLocation(request, ct);

                return Ok(new
                {
                    Message = "Target and location added successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("GetNearbyUsers")]
        public async Task<IActionResult> GetNearbyUsers([FromBody] NearbyUsersRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _geohashService.GetNearbyUsers(request, ct);

                return Ok(new
                {
                    Message = "Nearby users fetched successfully.",
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
