using Microsoft.AspNetCore.Http;

namespace GoBuddy.Application.DTOs;

public class VehicleDTO
{
    public string? VehicleNo { get; set; } 
    public string? VehicleModel { get; set; } 
    public string? LicenseNo { get; set; } 
    public int TotalSeats { get; set; }
    public IFormFile? LicenseImg { get; set; }
    public IFormFile? VehicleImg { get; set; }
}