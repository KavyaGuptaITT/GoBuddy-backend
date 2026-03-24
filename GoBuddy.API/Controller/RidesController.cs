using GoBuddy.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/rides")]
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

            var result = sessions.Select(session => new
            {
                SessionId = session.RideSessionId,
                Date = session.CreatedAt,
                CompletedAt = session.CompletedAt,
                TotalPassengers = session.RideRequests.Count,
                TotalEarnings = session.RideRequests.Sum(ride => ride.Fare),
                Passengers = session.RideRequests.Select(ride => new
                {
                    ride.PassengerId,
                    ride.PickupName,
                    ride.DropName,
                    ride.DistanceKm,
                    ride.Fare
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

            var result = requests.Select(ride => new
            {
                ride.RideRequestId,
                DriverName = ride.RideSession.Driver.Name,
                ride.PickupName,
                ride.DropName,
                ride.DistanceKm,
                ride.Fare,
                Date = ride.CreatedAt
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
