using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;
using OtakuQuest.Server.Services;
using System.Security.Claims;

namespace OtakuQuest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemController : ControllerBase
    {
        private readonly ItemService _itemService;

        public ItemController(ItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet("shop")]
        public async Task<ActionResult<List<Item>>> GetShopItems()
        {
            var items = await _itemService.GetShopItems();
            return Ok(items);
        }

        [HttpPost("buy")]
        public async Task<IActionResult> BuyItem([FromBody] BuyItemDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _itemService.BuyItem(userId.Value, dto);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(new { Message = $"Successfully purchased {result.Data!.Name}!" });
        }

        [HttpPost("equip")]
        public async Task<IActionResult> EquipItem([FromBody] EquipItemDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _itemService.EquipItem(userId.Value, dto);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(new { Message = $"Successfully equipped {result.Data!.Name}!" });
        }

        [HttpGet("inventory")]
        public async Task<ActionResult<List<Item>>> GetMyInventory()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _itemService.GetMyInventory(userId.Value);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto)
        {
            var newItem = await _itemService.CreateItem(dto);

            return Ok(new
            {
                Message = $"Successfully Added new item: {newItem.Name}",
                Item = newItem
            });
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
