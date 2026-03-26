using GoBuddy.Application.Interfaces;
using GoBuddy.BusinessLayer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GoBuddy.API.Controllers

{

    [Route("api/[controller]")]

    [ApiController]

    [Authorize]

    public class RidesController : ControllerBase
    {
        private readonly IRideRepository _rideRepository;


        public RidesController(IRideRepository rideRepository)

        {
            _rideRepository = rideRepository;

        }

        [HttpGet("driver/history")]
        [Authorize(Roles = "Driver")]

        public async Task<IActionResult> GetDriverRideHistory()

        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int driverId))

            {
                return Unauthorized(new { Message = "Invalid driver token." });

            }

            var sessions = await _rideRepository.GetDriverHistoryAsync(driverId);


            var historyList = sessions.Select(session => new DriverRideHistoryDto
            {
                RideSessionId = session.RideSessionId,

                StartLocation = session.StartName,

                DestinationLocation = session.DestinationName,

                TotalFare = session.TotalFare,

                TotalDistance = Math.Round(session.TotalDistance, 2),

                Status = session.Status,

                Date = session.CompletedAt ?? session.CreatedAt,

                Passengers = session.RideRequests.Select(request => new PassengerRideDetailsDto

                {

                    PassengerName = request.Passenger.Name,

                    PickupName = request.PickupName,

                    DropName = request.DropName,

                    Fare = request.Fare,

                    DistanceKm = Math.Round(request.DistanceKm, 2),

                    Status = request.Status

                }).ToList()

            }).ToList();


            return Ok(historyList);

        }

    }

}