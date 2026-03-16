using GoBuddy.API.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private static double CalculateDistance(double sourceLatitude, double sourceLongitude, double driverLatitude, double driverLongitude)
        {
            var latitudeDifference = driverLatitude - sourceLatitude;
            var longitudeDifference = driverLongitude - sourceLongitude;

            return Math.Sqrt(latitudeDifference * latitudeDifference + longitudeDifference * longitudeDifference) * 111.0;
        }

        [HttpGet("all")]
        public IActionResult GetAllDrivers()
        {
            var drivers = OnlineDriversStore.Drivers.Values
                .Select(driver => new
                {
                    driver.ConnectionId,
                    driver.DriverName,
                    driver.VehicleModel,
                    driver.Latitude,
                    driver.Longitude,
                    driver.IsBusy,
                    driver.AvailableSeats
                }).ToList();

            return Ok(drivers);
        }

        [HttpGet("nearby")]
        public IActionResult GetNearbyDrivers(
            [FromQuery] double passengerLatitude,
            [FromQuery] double passengerLongitude,
            [FromQuery] double searchRadiusKm = 2.0)
        {
            var nearbyDrivers = OnlineDriversStore.Drivers.Values
                .Where(driver => !driver.IsBusy &&
                    CalculateDistance(passengerLatitude, passengerLongitude, driver.Latitude, driver.Longitude) <= searchRadiusKm)
                .Select(driver => new
                {
                    driver.ConnectionId,
                    driver.DriverName,
                    driver.VehicleModel,
                    driver.Latitude,
                    driver.Longitude,
                    driver.AvailableSeats,
                    driver.RatePerKm,
                    DistanceKm = Math.Round(
                        CalculateDistance(passengerLatitude, passengerLongitude, driver.Latitude, driver.Longitude), 2)
                })
                .OrderBy(driver => driver.DistanceKm)
                .ToList();

            return Ok(nearbyDrivers);
        }
    }
}