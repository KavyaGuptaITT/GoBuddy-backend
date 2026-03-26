using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoBuddy.Domain.Entities

{

    public class RideSession
    {
        [Key]

        public int RideSessionId { get; private set; }


        public int DriverId { get; private set; }


        public int VehicleId { get; private set; }


        [ForeignKey("DriverId")]

        public User Driver { get; private set; } = null!;


        [ForeignKey("VehicleId")]

        public Vehicle Vehicle { get; private set; } = null!;


        public string StartName { get; private set; } = "";

        public double StartLatitude { get; private set; }

        public double StartLongitude { get; private set; }


        public string DestinationName { get; private set; } = "";

        public double DestinationLatitude { get; private set; }

        public double DestinationLongitude { get; private set; }


        public decimal TotalFare { get; private set; }

        public double TotalDistance { get; private set; }


        public string Status { get; private set; } = "Active";


        public DateTime CreatedAt { get; private set; }

        public DateTime? CompletedAt { get; private set; }


        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();


        public RideSession() { }


        public RideSession(

            int driverId,

            int vehicleId,

            string startName,

            double startLat,

            double startLng,

            string destName,

            double destLat,

            double destLng)

        {
            DriverId = driverId;

            VehicleId = vehicleId;


            StartName = startName;

            StartLatitude = startLat;

            StartLongitude = startLng;


            DestinationName = destName;

            DestinationLatitude = destLat;

            DestinationLongitude = destLng;


            TotalFare = 0;

            TotalDistance = 0;


            Status = "Active";

            CreatedAt = DateTime.UtcNow;

        }


        public void AddFare(decimal fare)

        {

            TotalFare += fare;

        }


        public void AddDistance(double km)

        {

            TotalDistance += km;

        }


        public void Complete()

        {

            Status = "Completed";

            CompletedAt = DateTime.UtcNow;

        }

    }

}