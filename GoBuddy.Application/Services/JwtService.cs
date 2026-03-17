using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace GoBuddy.BusinessLayer.Services
{
    public class JwtService
    {
        private readonly string secretKey;

        public JwtService(IConfiguration configuration)
        {
            secretKey = configuration["Jwt:Key"]!;
        }

        public string GenerateToken(string email, string role, string userId, string name, string vehicleModel, string userPin, int availableSeats, decimal ratePerKm)
        {
            byte[] key = Encoding.UTF8.GetBytes(secretKey);
            List<Claim> claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role),
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Name, name),
        new Claim("VehicleModel", vehicleModel),
        new Claim("UserPin", userPin),
        new Claim("AvailableSeats", availableSeats.ToString()),
        new Claim("RatePerKm", ratePerKm.ToString())

    };

            SigningCredentials credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}