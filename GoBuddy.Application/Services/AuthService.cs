using GoBuddy.BusinessLayer.DTOs;
using GoBuddy.BusinessLayer.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Domain.Enums;

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

            if (!Enum.TryParse(request.Role, out UserRole role))
            {
                throw new ApplicationException("Invalid Role");
            }

      User user = new User(
        request.Name,
        request.Phone,
        request.Email,
        hashedPassword,
        role
);

            await _userRepository.AddAsync(user);
        }
        public async Task<string> LoginAsync(LoginRequestDTO request)
        {
            User? user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new ApplicationException("User not found");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
                throw new ApplicationException("Invalid password");

            string token = _jwtService.GenerateToken(
                user.Email,
                user.Role.ToString(),
                user.PK_ID.ToString()
            );

            return token;
        }
    }
}
