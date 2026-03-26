using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoBuddy.Domain.Entities

{

    public class RideRequest
    {
        [Key]

        public int RideRequestId { get; private set; }


        public int RideSessionId { get; private set; }


        public int PassengerId { get; private set; }


        [ForeignKey("RideSessionId")]

        public RideSession RideSession { get; private set; } = null!;


        [ForeignKey("PassengerId")]

        public User Passenger { get; private set; } = null!;


        public string PickupName { get; private set; } = "";

        public double PickupLatitude { get; private set; }

        public double PickupLongitude { get; private set; }


        public string DropName { get; private set; } = "";

        public double DropLatitude { get; private set; }

        public double DropLongitude { get; private set; }


        public double DistanceKm { get; private set; }


        public decimal Fare { get; private set; }


        public string Status { get; private set; } = "Pending";


        public DateTime CreatedAt { get; private set; }

        public DateTime? CompletedAt { get; private set; }


        public RideRequest() { }


        public RideRequest(

            int sessionId,

            int passengerId,

            string pickupName,

            double pickupLat,

            double pickupLng,

            string dropName,

            double dropLat,

            double dropLng,

            double distanceKm,

            decimal fare)

        {
            RideSessionId = sessionId;

            PassengerId = passengerId;


            PickupName = pickupName;

            PickupLatitude = pickupLat;

            PickupLongitude = pickupLng;


            DropName = dropName;

            DropLatitude = dropLat;

            DropLongitude = dropLng;


            DistanceKm = distanceKm;

            Fare = fare;


            Status = "Pending";

            CreatedAt = DateTime.UtcNow;

        }


        public void Accept() => Status = "Accepted";


        public void Arrived() => Status = "Arrived";


        public void Verify() => Status = "Verified";


        public void Complete()

        {

            Status = "Completed";

            CompletedAt = DateTime.UtcNow;

        }


        public void Cancel() => Status = "Cancelled";

    }

}