using GoBuddy.API.Hubs;
using GoBuddy.API.SharedConstants;
using Microsoft.AspNetCore.Mvc;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/drivers")]
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

        [HttpGet("GetAllDrivers")]
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

        [HttpGet("GetAllNearbyDrivers")]
        public IActionResult GetNearbyDrivers(
            [FromQuery] double passengerLatitude,
            [FromQuery] double passengerLongitude,
            [FromQuery] double searchRadiusKm = AppConstants.DefaultSearchRadiusKm)
        {
            var nearbyDrivers = OnlineDriversStore.Drivers.Values
                .Where(driver =>
                    !driver.IsBusy && driver.AvailableSeats > 0 &&
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