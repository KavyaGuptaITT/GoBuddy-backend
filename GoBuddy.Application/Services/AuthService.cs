using GoBuddy.BusinessLayer.Interfaces;
using GoBuddy.BusinessLayer.DTOs;
using GoBuddy.Domain.Entities;
using BCrypt.Net;

namespace GoBuddy.BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;

        public AuthService(IUserRepository userRepository, JwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task RegisterAsync(RegisterRequestDTO request)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            User user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            await _userRepository.AddAsync(user);
        }

        public async Task<string> LoginAsync(LoginRequestDTO request)
        {
            User? user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                throw new Exception("User not found");
                
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new Exception("Invalid password");
            }

            string token = _jwtService.GenerateToken(
                user.Email,
                user.Role,
                user.Id.ToString()
            );

            return token;
        }
    }
}
