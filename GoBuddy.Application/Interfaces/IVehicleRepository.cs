using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle);
    Task<List<Vehicle>> GetAllAsync();
    Task<bool> DriverHasActiveVehicleAsync(int driverId);
    Task<Vehicle?> GetByDriverIdAsync(int driverId);
    Task UpdateAsync(Vehicle vehicle);
}