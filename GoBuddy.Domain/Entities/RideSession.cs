using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GoBuddy.Domain.Entities;

namespace GoBuddy.Domain.Entities
{
    public class RideSession
    {
        [Key]
        public int PK_ID { get; private set; }

        public int FK_Driver_ID { get; private set; }

        [ForeignKey("FK_Driver_ID")]
        public User Driver { get; private set; } = null!;

        public string Status { get; private set; }
        public double CurrentLatitude { get; private set; }
        public double CurrentLongitude { get; private set; }
        public int TotalPassengersInRide { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();

        public RideSession() {}

        public RideSession(int driverId, string status, double currentLatitude, double currentLongitude, int totalPassengersInRide)
        {
        
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty");

            if (currentLatitude < -90 || currentLatitude > 90)
                throw new ArgumentException("CurrentLatitude must be between -90 and 90");

            if (currentLongitude < -180 || currentLongitude > 180)
                throw new ArgumentException("CurrentLongitude must be between -180 and 180");


            FK_Driver_ID = driverId;
            Status = status;
            CurrentLatitude = currentLatitude;
            CurrentLongitude = currentLongitude;
            TotalPassengersInRide = totalPassengersInRide;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLocation(double latitude, double longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90");

            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180");

            CurrentLatitude = latitude;
            CurrentLongitude = longitude;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}