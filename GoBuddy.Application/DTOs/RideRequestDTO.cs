using System;
using System.Collections.Generic;
using System.Text;

namespace GoBuddy.API.DTOs
{
    public class RideRequestDto
    {
        public string DriverConnectionId { get; set; } = string.Empty;
        public string PassengerId { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        public double DropLatitude { get; set; }
        public double DropLongitude { get; set; }

        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;


        public string PassengerPin { get; set; } = string.Empty;
    }
}
