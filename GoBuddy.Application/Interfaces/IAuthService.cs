using GoBuddy.BusinessLayer.DTOs;

namespace GoBuddy.BusinessLayer.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequestDTO request);

        Task<string> LoginAsync(LoginRequestDTO request);
    }
}
