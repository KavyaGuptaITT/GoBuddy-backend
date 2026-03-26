using GoBuddy.Application.DTOs;
using GoBuddy.BusinessLayer.DTOs;
using GoBuddy.BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace GoBuddy.API.Controllers
{
    
    [Route("api/auth")]
    [ApiController]         
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("register")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
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
        [Authorize(Roles = "Driver,Passenger")]
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

