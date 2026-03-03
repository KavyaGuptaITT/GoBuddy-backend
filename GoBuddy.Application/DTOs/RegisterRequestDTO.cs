
using Microsoft.AspNetCore.Http;

namespace GoBuddy.BusinessLayer.DTOs
{
    public class RegisterRequestDTO
    {
        public string? Name { get; set; } 
        public string? Phone { get; set; } 
        public string? Email { get; set; } 
        public string? Password { get; set; } 
        public string? Role { get; set; } 
        public DateTime Dob { get; set; }
        public string? VehicleNumber { get; set; }
        public string? VehicleModel { get; set; }
        public string? LicenseNumber { get; set; }
        public int? TotalSeats { get; set; }
        //public IFormFile? LicenseImg { get; set; }
       // public IFormFile? VehicleImg { get; set; }

    }
}


