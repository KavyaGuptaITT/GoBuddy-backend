using GoBuddy.Domain.Enums;
using System.ComponentModel.DataAnnotations;
namespace GoBuddy.Domain.Entities
{

    public class User
    {
        [Key]
        public int PK_ID { get; private set; }
        public string Name { get; private set; }
        public string Phone { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
        public ICollection<RideSession> RideSessions { get; private set; } = new List<RideSession>();
        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();

        public User() {}

        public User(string name, string phone, string email, string passwordHash, UserRole role)
        {
         
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password cannot be empty");

            Name = name;
            Phone = phone;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}