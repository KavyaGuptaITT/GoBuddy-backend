using GoBuddy.Application.Interfaces;
using GoBuddy.BusinessLayer.DTOs;
using GoBuddy.BusinessLayer.Interfaces;
using GoBuddy.Domain.Entities;
using GoBuddy.Domain.Enums;

namespace GoBuddy.BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVehicleRepository _vehicleRepository;   
        private readonly JwtService _jwtService;

        public AuthService(
            IUserRepository userRepository,
            IVehicleRepository vehicleService,   
            JwtService jwtService)
        {
            _userRepository = userRepository;
            _vehicleRepository = vehicleService; 
            _jwtService = jwtService;
        }

        public async Task RegisterAsync(RegisterRequestDTO request)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            if (!Enum.TryParse(request.Role, true, out UserRole role))
            {
                throw new ApplicationException("Invalid Role");
            }

            User user = new User(
                request.Name,
                request.Phone,
                request.Email,
                hashedPassword,
                role,
                request.Dob
            );

            await _userRepository.AddAsync(user);

            if (role == UserRole.Driver)
            {
                if (string.IsNullOrWhiteSpace(request.VehicleNumber) ||
                    string.IsNullOrWhiteSpace(request.VehicleModel) ||
                    string.IsNullOrWhiteSpace(request.LicenseNumber) ||
                    request.TotalSeats == null)
                {
                    throw new ApplicationException("Vehicle details required for Driver");
                }

                var vehicle = new Vehicle(
                    user.UserId,
                    request.VehicleNumber,
                    request.VehicleModel,
                    request.LicenseNumber,
                    request.TotalSeats.Value
                );

                await _vehicleRepository.AddAsync(vehicle);
            }
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
                user.UserId.ToString()
            );

            return token;
        }
    }
}


