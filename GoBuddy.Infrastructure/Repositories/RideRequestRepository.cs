using GoBuddy.Application.Interfaces.Repositories;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Repositories
{
    public class RideRequestRepository : IRideRequestRepository
    {
        private readonly AppDbContext context;

        public RideRequestRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<RideRequest?> GetByIdAsync(int id)
        {
            return await context.RideRequests
                .FirstOrDefaultAsync(x => x.RideRequestId == id);
        }

        public async Task<List<RideRequest>> GetByRideSessionIdAsync(int rideSessionId)
        {
            return await context.RideRequests
                .Where(x => x.RideSessionId == rideSessionId)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}