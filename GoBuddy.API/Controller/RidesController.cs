using GoBuddy.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RidesController : ControllerBase
    {
        private readonly IRideRepository _rideRepository;

        public RidesController(IRideRepository rideRepository)
        {
            _rideRepository = rideRepository;
        }

        [HttpGet("driver")]
        public async Task<IActionResult> GetDriverRides()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var sessions = await _rideRepository.GetDriverRidesAsync(userId);

            var result = sessions.Select(s => new
            {
                SessionId = s.RideSessionId,
                Date = s.CreatedAt,
                CompletedAt = s.CompletedAt,
                TotalPassengers = s.RideRequests.Count,
                TotalEarnings = s.RideRequests.Sum(r => r.Fare),
                Passengers = s.RideRequests.Select(r => new
                {
                    r.PassengerId,
                    r.PickupName,
                    r.DropName,
                    r.DistanceKm,
                    r.Fare
                })
            });

            return Ok(result);
        }

        [HttpGet("passenger")]
        public async Task<IActionResult> GetPassengerRides()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var requests = await _rideRepository.GetPassengerRidesAsync(userId);

            var result = requests.Select(r => new
            {
                r.RideRequestId,
                DriverName = r.RideSession.Driver.Name,
                r.PickupName,
                r.DropName,
                r.DistanceKm,
                r.Fare,
                Date = r.CreatedAt
            });

            return Ok(result);
        }
    }
}
namespace GoBuddy.API.Controller
{
    public class RidesController
    {
    }
}
