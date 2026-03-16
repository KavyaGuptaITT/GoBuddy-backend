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

        public double PickupLat { get; set; }
        public double PickupLng { get; set; }

        public double DropLat { get; set; }
        public double DropLng { get; set; }

        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;

        public string PassengerPin { get; set; } = string.Empty;
    }
}
