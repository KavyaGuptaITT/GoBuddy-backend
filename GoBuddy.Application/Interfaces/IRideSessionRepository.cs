using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces
{
    public interface IRideSessionRepository
    {
        Task<RideSession?> GetByDriverIdAsync(int driverId);
        Task<List<RideSession>> GetActiveSessionsAsync();
        Task SaveChangesAsync();
    }
}