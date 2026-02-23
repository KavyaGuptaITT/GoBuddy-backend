using GoBuddy.Application.DTOs;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace GoBuddy.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext dbContext;

    public VehiclesController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicle(CreateVehicleRequest request)
    {
        var userRole = "Driver";
        var driverId = Guid.NewGuid();

        if (userRole != "Driver")
            return StatusCode(403);

        if (request.TotalSeats < 1 || request.TotalSeats > 6)
            return BadRequest("TotalSeats must be between 1 and 6");

        var existingVehicle = dbContext.Vehicles
            .FirstOrDefault(v => v.DriverId == driverId && v.IsActive);

        if (existingVehicle != null)
            return BadRequest("Driver already has active vehicle");

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            DriverId = driverId,
            VehicleNo = request.VehicleNo,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.TotalSeats,
            IsActive = true
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();

        return Ok(vehicle);
    }

    [HttpGet]
    public IActionResult GetVehicles()
    {
        var vehicles = dbContext.Vehicles.ToList();
        return Ok(vehicles);
    }
}