using GoBuddy.Application.DTOs;
using GoBuddy.BusinessLayer.DTOs;
using GoBuddy.BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace GoBuddy.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequestDTO request)
        {
            await _authService.RegisterAsync(request);

            return Ok(new
            {
                success = true,
                message = "User registered successfully"
            });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            string token = await _authService.LoginAsync(request);

            return Ok(new
            {
                success = true,
                token = token
            });
        }
    }
}

