using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GoBuddy.Domain.Entities;
using GoBuddy.Domain.Enums;

namespace GoBuddy.Domain.Entities
{
    public class RideSession
    {
        [Key]
        public int RideSessionId { get; private set; }

        public int DriverId { get; private set; }

        [ForeignKey("DriverId")]
        public User Driver { get; private set; } = null!;

        public RideSessionStatus Status { get; private set; }
        public double CurrentLatitude { get; private set; }
        public double CurrentLongitude { get; private set; }
        public int TotalPassengersInRide { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();

        public RideSession(int driverId, RideSessionStatus status, double currentLatitude, double currentLongitude, int totalPassengersInRide)
        {
        
            if (!Enum.IsDefined(typeof(RideSessionStatus), status))
                throw new ArgumentException("Invalid status");

            if (currentLatitude < -90 || currentLatitude > 90)
                throw new ArgumentException("CurrentLatitude must be between -90 and 90");

            if (currentLongitude < -180 || currentLongitude > 180)
                throw new ArgumentException("CurrentLongitude must be between -180 and 180");


            DriverId = driverId;
            Status = status;
            CurrentLatitude = currentLatitude;
            CurrentLongitude = currentLongitude;
            TotalPassengersInRide = totalPassengersInRide;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }     
         
         public void Cancel()
        {
            if (Status == RideSessionStatus.Cancelled ||
                 Status == RideSessionStatus.Completed)
                 throw new InvalidOperationException("Ride session cannot be cancelled");

            Status = RideSessionStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}