using GoBuddy.BusinessLayer.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            User? user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Email == email);
            return user;
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
    }
}
