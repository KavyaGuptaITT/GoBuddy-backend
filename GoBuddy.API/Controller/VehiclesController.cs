using GoBuddy.Application.DTOs;
using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/Vehicles")]
[ApiController]           
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpPut]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> AddVehicle([FromForm] VehicleDTO request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        string userIdValue = User.FindFirst("UserId")?.Value ?? "0";
        int driverId = int.Parse(userIdValue);

        await _vehicleService.AddVehicleAsync(driverId, role, request);

        return Ok(new { message = "Vehicle created successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Driver,Passenger")]
    public async Task<IActionResult> GetVehicles()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }
}