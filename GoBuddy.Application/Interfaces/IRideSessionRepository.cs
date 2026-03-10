using GoBuddy.Domain.Entities;

namespace GoBuddy.Application.Interfaces.Repositories
{
    public interface IRideSessionRepository
    {
        Task<RideSession?> GetByIdAsync(int id);
        Task<RideSession?> GetByDriverIdAsync(int driverId);
        Task SaveChangesAsync();
    }
}