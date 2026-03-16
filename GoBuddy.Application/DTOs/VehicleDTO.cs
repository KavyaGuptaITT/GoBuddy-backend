using Microsoft.AspNetCore.Http;

namespace GoBuddy.Application.DTOs
{
    public class VehicleDTO
    {
        public string VehicleNo { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public string LicenseNo { get; set; } = null!;
        public int TotalSeats { get; set; }
        public int RatePerKm { get; set; } = 0;
        public IFormFile? LicenseImg { get; set; }
        public IFormFile? VehicleImg { get; set; }
    }
}