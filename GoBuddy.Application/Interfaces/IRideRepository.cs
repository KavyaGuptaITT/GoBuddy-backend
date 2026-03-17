using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces
{
    public interface IRideRepository
    {
        Task<int> CreateSessionAsync(RideSession session);
        Task CreateRequestAsync(RideRequest request);
        Task<List<RideSession>> GetDriverRidesAsync(int driverId);
        Task<List<RideRequest>> GetPassengerRidesAsync(int passengerId);
    }
}