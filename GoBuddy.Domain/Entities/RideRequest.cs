using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GoBuddy.Domain.Entities;
 
namespace GoBuddy.Domain.Entities
{
    public class RideRequest
    {
        [Key]
        public int RideRequestId { get; private set; }
 
        public int RideSessionId { get; private set; }
        public int PassengerId { get; private set; }
 
        [ForeignKey("RideSessionId")]
        public RideSession RideSession { get; private set; }
 
        [ForeignKey("PassengerId")]
        public User Passenger { get; private set; }
 
        public double PickupLatitude { get; private set; }
        public double PickupLongitude { get; private set; }
 
        public double DropupLatitude { get; private set; }
        public double DropupLongitude { get; private set; }
 
        public string Status { get; private set; }
 
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
 
        public RideRequest() {}
 
        public RideRequest(
            int rideSessionId,
            int passengerId,
            double pickupLatitude,
            double pickupLongitude,
            double dropupLatitude,
            double dropupLongitude,
            string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty");
 
            if (pickupLatitude < -90 || pickupLatitude > 90)
                throw new ArgumentException("PickupLatitude must be between -90 and 90");
 
            if (pickupLongitude < -180 || pickupLongitude > 180)
                throw new ArgumentException("PickupLongitude must be between -180 and 180");
 
            if (dropupLatitude < -90 || dropupLatitude > 90)
                throw new ArgumentException("DropupLatitude must be between -90 and 90");
 
            if (dropupLongitude < -180 || dropupLongitude > 180)
                throw new ArgumentException("DropupLongitude must be between -180 and 180");
 
            RideSessionId = rideSessionId;
            PassengerId = passengerId;
            PickupLatitude = pickupLatitude;
            PickupLongitude = pickupLongitude;
            DropupLatitude = dropupLatitude;
            DropupLongitude = dropupLongitude;
            Status = status;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}