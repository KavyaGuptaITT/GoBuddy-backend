using Microsoft.AspNetCore.Http;

namespace GoBuddy.BusinessLayer.DTOs
{
    public class RegisterRequestDTO
    {
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime Dob { get; set; }


        public string? VehicleNumber { get; set; }
        public string? VehicleModel { get; set; }
        public string? LicenseNumber { get; set; }
        public int? TotalSeats { get; set; }
        public decimal? RatePerKm { get; set; }
        public IFormFile? LicenseImg { get; set; }
        public IFormFile? VehicleImg { get; set; }
    }
}
