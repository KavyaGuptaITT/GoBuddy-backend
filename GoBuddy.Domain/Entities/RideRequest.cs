using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GoBuddy.Domain.Entities;
using GoBuddy.Domain.Enums;

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

        public double DropupLatitude { get; private set; }
        public double DropupLongitude { get; private set; }

        public RideRequestStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private RideRequest() { }

        public RideRequest(int rideSessionId, int passengerId, double pickupLatitude, double pickupLongitude,double dropupLatitude, double dropupLongitude, RideRequestStatus status)
        {
           
            if (!Enum.IsDefined(typeof(RideSessionStatus), status))
               throw new ArgumentException("Invalid status");

            if (pickupLatitude < -90 || pickupLatitude > 90)
               throw new ArgumentException("PickupLatitude must be between -90 and 90");

            if (pickupLongitude < -180 || pickupLongitude > 180)
                throw new ArgumentException("PickupLongitude must be between -180 and 180");

            if (dropupLatitude < -90 || dropupLatitude > 90)
                throw new ArgumentException("PickupLatitude must be between -90 and 90");

            if (dropupLongitude < -180 || dropupLongitude > 180)
                throw new ArgumentException("PickupLongitude must be between -180 and 180");

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

        public void Cancel()
       {
            if (Status == RideRequestStatus.Cancelled ||
               Status == RideRequestStatus.Completed ||
               Status == RideRequestStatus.Boarded)
               throw new InvalidOperationException("Ride request cannot be cancelled");

           Status = RideRequestStatus.Cancelled;
           UpdatedAt = DateTime.UtcNow;
       }
    }
}