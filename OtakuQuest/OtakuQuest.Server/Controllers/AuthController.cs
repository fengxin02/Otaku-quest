using Microsoft.AspNetCore.Mvc;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Services;

namespace OtakuQuest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")] // POST /api/auth/register 
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.Register(dto);
            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }
            return Ok(new { Message = result.Data });
        }

        [HttpPost("login")]
        public ActionResult<AuthResponseDto> Login([FromBody] LoginDto dto)
        {
            var result = _authService.Login(dto);
            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Data);
        }
    }
}
