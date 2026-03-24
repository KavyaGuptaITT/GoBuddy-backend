using GoBuddy.Application.DTOs;
using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces;

public interface IVehicleService
{
    Task AddVehicleAsync(int driverId, string role, VehicleDTO request);
    Task<List<Vehicle>> GetAllVehiclesAsync();
}