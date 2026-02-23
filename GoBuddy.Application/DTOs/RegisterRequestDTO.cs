
namespace GoBuddy.BusinessLayer.DTOs
{
    public class RegisterRequestDTO
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
