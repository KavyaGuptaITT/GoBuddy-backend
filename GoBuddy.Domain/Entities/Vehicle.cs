namespace GoBuddy.Domain.Entities;

public class Vehicle
{
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public string VehicleNo { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public bool IsActive { get; set; } = true;
}