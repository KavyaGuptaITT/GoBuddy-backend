using GoBuddy.API.Hubs;
using GoBuddy.API.SharedConstants;
using Microsoft.AspNetCore.Mvc;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private static double CalculateDistance(
            double sourceLatitude,
            double sourceLongitude,
            double driverLatitude,
            double driverLongitude)
        {
            var latitudeDifference = driverLatitude - sourceLatitude;
            var longitudeDifference = driverLongitude - sourceLongitude;

            return Math.Sqrt(latitudeDifference * latitudeDifference +
                             longitudeDifference * longitudeDifference)
                             * AppConstants.KmConversionFactor;
        }

        [HttpGet("getAllDrivers")]
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
                })
                .ToList();

            return Ok(drivers);
        }

        [HttpGet("getNearbyDrivers")]
        public IActionResult GetNearbyDrivers(
            [FromQuery] double passengerLatitude,
            [FromQuery] double passengerLongitude,
            [FromQuery] double searchRadiusKm = AppConstants.DefaultSearchRadiusKm)
        {
            var nearbyDrivers = OnlineDriversStore.Drivers.Values
                .Where(driver =>
                    !driver.IsBusy &&
                    CalculateDistance(
                        passengerLatitude,
                        passengerLongitude,
                        driver.Latitude,
                        driver.Longitude) <= searchRadiusKm)
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
                        CalculateDistance(
                            passengerLatitude,
                            passengerLongitude,
                            driver.Latitude,
                            driver.Longitude),
                        AppConstants.DistanceRoundingPrecision)
                })
                .OrderBy(driver => driver.DistanceKm)
                .ToList();

            return Ok(nearbyDrivers);
        }
    }
}