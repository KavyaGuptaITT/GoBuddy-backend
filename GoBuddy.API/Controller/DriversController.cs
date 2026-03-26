using GoBuddy.API.Hubs;
using GoBuddy.API.SharedConstants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Timers;

namespace GoBuddy.API.Controllers

{

    [ApiController]
    [Route("api/drivers")]
    [Authorize]

    public class DriversController : ControllerBase
    {
        [HttpGet("GetAllDrivers")]
        [Authorize(Roles = "Driver")]

        public IActionResult GetAllDrivers()

        {
            var drivers = OnlineDriversStore.Drivers.Values

                .Select(driver => new {
                    driver.ConnectionId,
                    driver.DriverName,
                    driver.VehicleModel,
                    driver.Latitude,
                    driver.Longitude,
                    driver.AvailableSeats

                }).ToList();

            return Ok(drivers);

        }

        [HttpGet("GetAllNearbyDrivers")]
        [Authorize(Roles = "Passenger")]

        public IActionResult GetNearbyDrivers(

            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double dropLatitude,
            [FromQuery] double dropLongitude,
            [FromQuery] double radiusKm=3)

        {
            var nearby = OnlineDriversStore.Drivers.Values

                .Where(driver => driver.AvailableSeats > 0)

                .Select(driver => new {
                    driver.ConnectionId,
                    driver.DriverName,
                    driver.VehicleModel,

                    driver.Latitude,
                    driver.Longitude,
                    driver.AvailableSeats,
                    driver.RatePerKm,
                    DistanceKm = Math.Round(GetDistanceKm(driver.Latitude, driver.Longitude, latitude, longitude), 2)
                }).Where(driver => driver.DistanceKm <= radiusKm)

                .Where(driver =>
                {  
                    
                    var boardedRides = OnlineDriversStore.ActiveRides.Values
                        .Where(ride => ride.DriverConnectionId == driver.ConnectionId && ride.PinConfirmed)
                        .ToList();


                    if (!boardedRides.Any()) return true;


                    return boardedRides.Any(ride => GetDistanceKm(ride.DropLat, ride.DropLng, dropLatitude, dropLongitude) <= 3.0);

                }).OrderBy(driver => driver.DistanceKm)

                .ToList();


            return Ok(nearby);

        }


        private static double GetDistanceKm(double lat1, double lng1, double lat2, double lng2)

        {

            double dx = lat2 - lat1;

            double dy = (lng2 - lng1) * Math.Cos(lat1 * Math.PI / 180);

            return Math.Sqrt(dx * dx + dy * dy) * 111.0;

        }

    }

}