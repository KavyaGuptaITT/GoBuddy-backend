using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces
{
    public interface IRideRepository
    {
        Task<int> AddSessionAsync(RideSession session);
        Task AddRequestAsync(RideRequest request);
        Task<List<RideSession>> GetDriverRidesAsync(int driverId);
        Task<List<RideRequest>> GetPassengerRidesAsync(int passengerId);
        Task<RideSession?> GetActiveSessionByDriverIdAsync(int driverId);
        Task<RideSession?> GetSessionByIdAsync(int sessionId);
        Task UpdateSessionAsync(RideSession session);
        Task<IEnumerable<RideSession>> GetDriverHistoryAsync(int driverId);
    }
}