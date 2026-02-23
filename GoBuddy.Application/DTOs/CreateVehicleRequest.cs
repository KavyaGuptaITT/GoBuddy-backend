namespace GoBuddy.Application.DTOs;

public class CreateVehicleRequest
{
    public string VehicleNo { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}