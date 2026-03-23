using System.Threading.Tasks;
using GoBuddy.Domain.Entities;

namespace GoBuddy.BusinessLayer.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
    }
}
