using GoBuddy.Application.DTOs;
using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Domain.Enums;

namespace GoBuddy.BusinessLayer.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task CreateVehicleAsync(int driverId, string role, VehicleDTO request)
    {
        if (role != UserRole.Driver.ToString())
            throw new ApplicationException("Only drivers can add vehicles");

        bool alreadyExists = await _vehicleRepository.DriverHasActiveVehicleAsync(driverId);
        if (alreadyExists)
            throw new ApplicationException("Driver already has an active vehicle");

        if (request.TotalSeats < 1 || request.TotalSeats > 6)
            throw new ApplicationException("Total seats must be between 1 and 6");

        Vehicle vehicle = new Vehicle(driverId, request.VehicleNo, request.TotalSeats);

        await _vehicleRepository.AddAsync(vehicle);
    }

    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        return await _vehicleRepository.GetAllAsync();
    }
}