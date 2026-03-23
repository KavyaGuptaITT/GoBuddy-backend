using GoBuddy.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
 
namespace GoBuddy.Domain.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; private set; }
        [Required]
        public string Name { get; private set; }
 
        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        public string Phone { get; private set; }
 
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; private set; }
 
        [Required]
        public string PasswordHash { get; private set; }
 
        [Required]
        public UserRole Role { get; private set; }
 
        [Required]
        public DateTime Dob { get; private set; }
 
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public int UserPin { get; private set; }
 
        public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
        public ICollection<RideSession> RideSessions { get; private set; } = new List<RideSession>();
        public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();
 
        public User() { }
 
        public User(string name, string phone, string email, string passwordHash, UserRole role, DateTime dob)
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
            Dob = dob;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UserPin = GenerateRandomPin();
        }
 
        private static int GenerateRandomPin() => RandomNumberGenerator.GetInt32(1000, 10000);
 
        public void ResetPin()
        {
            UserPin = GenerateRandomPin();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
