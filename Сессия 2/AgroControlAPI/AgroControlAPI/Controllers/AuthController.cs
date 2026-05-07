using Microsoft.AspNetCore.Mvc;
using AgroControlAPI.DTOs;
using AgroControlAPI.Services;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var user = await _authService.Register(registerDto);
                return Ok(new { success = true, message = "Регистрация успешна", userId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var (user, token) = await _authService.Authenticate(loginDto.Username, loginDto.Password);

            if (user == null)
                return Unauthorized(new { success = false, message = "Неверный логин или пароль" });

            return Ok(new AuthResponseDto
            {
                Id = (int)user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Department = user.Department,
                Token = token
            });
        }
    }
}