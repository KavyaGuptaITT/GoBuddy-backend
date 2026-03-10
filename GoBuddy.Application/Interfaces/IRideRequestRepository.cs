using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces.Repositories
{
    public interface IRideRequestRepository
    {
        Task<RideRequest?> GetByIdAsync(int id);
        Task<List<RideRequest>> GetByRideSessionIdAsync(int rideSessionId);
        Task SaveChangesAsync();
    }
}