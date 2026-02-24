using GoBuddy.Application.DTOs;
using GoBuddy.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
    {
        private readonly IRideLocationService _locationService;

        public LocationController(IRideLocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpPut]
        public async Task<IActionResult> UpdateLocation(RideLocationDTO request)
        {
            string userIdValue = User.FindFirst("UserId")?.Value ?? "0";
            int driverId = int.Parse(userIdValue);

            await _locationService.UpdateLocationAsync(driverId, request.Latitude, request.Longitude);

            return Ok(new { message = "Location updated successfully" });
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyDrivers(double latitude, double longitude, double radiusInKm)
        {
            var drivers = await _locationService.GetNearbyDriversAsync(latitude, longitude, radiusInKm);
            return Ok(drivers);
        }
    }
}