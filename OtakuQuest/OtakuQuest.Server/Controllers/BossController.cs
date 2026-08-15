using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Services;
using System.Security.Claims;

namespace OtakuQuest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BossController : ControllerBase
    {
        private readonly BossService _bossService;

        public BossController(BossService bossService)
        {
            _bossService = bossService;
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateBoss([FromBody] CreateBossDto dto)
        {
            var boss = await _bossService.CreateBoss(dto);
            return Ok(boss);
        }

        [HttpGet("current")]
        public async Task<ActionResult<CurrentBossResponseDto>> GetCurrentBoss()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _bossService.GetCurrentBoss(userId.Value);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost("attack")]
        public async Task<ActionResult<CombatResultDto>> AttackBoss()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _bossService.AttackBoss(userId.Value);
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
