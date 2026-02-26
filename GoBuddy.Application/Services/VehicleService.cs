using GoBuddy.Application.DTOs;
using GoBuddy.Application.Helpers;
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

        Vehicle vehicle = new Vehicle(
            driverId,
            request.VehicleNo,
            request.VehicleModel,
            request.LicenseNo,
            request.TotalSeats
        );

        if (request.VehicleImg != null && request.VehicleImg.Length > 3 * 1024 * 1024)
            throw new ApplicationException("Vehicle image size must be less than 3MB");

        if (request.LicenseImg != null && request.LicenseImg.Length > 3 * 1024 * 1024)
            throw new ApplicationException("License image size must be less than 3MB");

        byte[] vehicleBytes = await FileHelper.ConvertToBytes(request.VehicleImg);
        byte[] licenseBytes = await FileHelper.ConvertToBytes(request.LicenseImg);

        vehicle.SetImages(vehicleBytes, licenseBytes);

        await _vehicleRepository.AddAsync(vehicle);
    }

    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        return await _vehicleRepository.GetAllAsync();
    }
}