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
        public double PickupLatitude { get; private set; }
        public double PickupLongitude { get; private set; }
        public double DropLatitude { get; private set; }
        public double DropLongitude { get; private set; }
        public string PickupName { get; private set; } = string.Empty;
        public string DropName { get; private set; } = string.Empty;
        public double DistanceKm { get; private set; }
        public decimal Fare { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public RideRequest() { }

        public RideRequest(
            int rideSessionId, int passengerId,
            double pickupLat, double pickupLng,
            double dropLat, double dropLng,
            string pickupName, string dropName,
            double distanceKm, decimal fare)
        {
            RideSessionId = rideSessionId;
            PassengerId = passengerId;
            PickupLatitude = pickupLat;
            PickupLongitude = pickupLng;
            DropLatitude = dropLat;
            DropLongitude = dropLng;
            PickupName = pickupName;
            DropName = dropName;
            DistanceKm = distanceKm;
            Fare = fare;
            Status = "Completed";
            CreatedAt = DateTime.UtcNow;
            CompletedAt = DateTime.UtcNow;
        }
    }
}