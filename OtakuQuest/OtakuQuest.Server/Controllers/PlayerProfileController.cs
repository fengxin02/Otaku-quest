using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Services;
using System.Security.Claims;

namespace OtakuQuest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] //this makes this controller protected, only authenticated users can access it JWT
    public class PlayerProfileController : ControllerBase
    {
        private readonly PlayerProfileService _playerProfileService;

        public PlayerProfileController(PlayerProfileService playerProfileService)
        {
            _playerProfileService = playerProfileService;
        }

        [HttpGet("my-stats")]
        public async Task<ActionResult<PlayerStatsDto>> GetStats()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _playerProfileService.GetStats(userId.Value);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(result.Data);
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null)
            {
                return null;
            }
            return int.Parse(userIdString);
        }
    }
}
