using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles.ToListAsync();
    }

    public async Task<bool> DriverHasActiveVehicleAsync(int driverId)
    {
        return await _context.Vehicles
            .AnyAsync(vehicle => vehicle.FK_user_ID == driverId && vehicle.IsActive);
    }
}