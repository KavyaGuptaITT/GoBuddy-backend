using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces
{
    public interface IRideLocationService
    {
        Task UpdateLocationAsync(int driverId, double latitude, double longitude);
        Task<List<RideSession>> GetNearbyDriversAsync(double latitude, double longitude, double radiusInKm);
    }
}