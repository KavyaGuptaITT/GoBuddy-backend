
namespace GoBuddy.BusinessLayer.DTOs
{
    public class RegisterRequestDTO
    {
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;   
        public DateTime Dob { get; set; }           
    }
}
