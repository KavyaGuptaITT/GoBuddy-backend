using GoBuddy.Domain.Entities;

namespace GoBuddy.BusinessLayer.DTOs
{
    public class DriverRideHistoryDto
    {
        public int RideSessionId { get; set; }
        public string StartLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public decimal TotalFare { get; set; }
        public double TotalDistance { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<PassengerRideDetailsDto> Passengers { get; set; } = new();
    }
    public class PassengerRideDetailsDto
    {
        public string PassengerName { get; set; } = string.Empty;
        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;
        public decimal Fare { get; set; }
        public double DistanceKm { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
 
