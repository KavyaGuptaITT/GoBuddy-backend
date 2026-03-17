using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Repositories
{
    public class RideRepository : IRideRepository
    {
        private readonly AppDbContext _context;

        public RideRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateSessionAsync(RideSession session)
        {
            await _context.RideSessions.AddAsync(session);
            await _context.SaveChangesAsync();
            return session.RideSessionId;
        }

        public async Task CreateRequestAsync(RideRequest request)
        {
            await _context.RideRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RideSession>> GetDriverRidesAsync(int driverId)
        {
            return await _context.RideSessions
                .Where(s => s.DriverId == driverId && s.Status == "Completed")
                .Include(s => s.RideRequests)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RideRequest>> GetPassengerRidesAsync(int passengerId)
        {
            return await _context.RideRequests
                .Where(r => r.PassengerId == passengerId)
                .Include(r => r.RideSession)
                    .ThenInclude(s => s.Driver)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}