using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoBuddy.Domain.Entities
{
    public class RideSession
    {
        [Key]
        public int RideSessionId { get; private set; }
        public int DriverId { get; private set; }
        [ForeignKey("DriverId")]
        public User Driver { get; private set; } = null!;
        public string Status { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();

        public RideSession() { }

        public RideSession(int driverId)
        {
            DriverId = driverId;
            Status = "Active";
            CreatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            Status = "Completed";
            CompletedAt = DateTime.UtcNow;
        }
    }
}