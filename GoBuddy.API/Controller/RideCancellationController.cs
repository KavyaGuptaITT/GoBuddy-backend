using Microsoft.AspNetCore.Mvc;
using GoBuddy.BusinessLayer.Services;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/ride")]
    public class RideCancellationController : ControllerBase
    {
        private readonly RideCancellationService service;

        public RideCancellationController(RideCancellationService service)
        {
            this.service = service;
        }

        [HttpPost("passenger-cancel/{rideRequestId}")]
        public async Task<IActionResult> PassengerCancel(int rideRequestId)
        {
            await service.CancelRideByPassengerAsync(rideRequestId);
            return Ok();
        }

        [HttpPost("driver-cancel/{driverId}")]
        public async Task<IActionResult> DriverCancel(int driverId)
        {
            await service.CancelRideByDriverAsync(driverId);
            return Ok();
        }
    }
}