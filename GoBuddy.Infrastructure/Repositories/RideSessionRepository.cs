using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Repositories
{
    public class RideSessionRepository : IRideSessionRepository
    {
        private readonly AppDbContext _context;

        public RideSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RideSession?> GetByDriverIdAsync(int driverId)
        {
            return await _context.RideSessions
                .FirstOrDefaultAsync(rideSession => rideSession.FK_Driver_ID == driverId && rideSession.Status == "Active");
        }

        public async Task<List<RideSession>> GetActiveSessionsAsync()
        {
            return await _context.RideSessions
                .Where(rideSession => rideSession.Status == "Active")
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}