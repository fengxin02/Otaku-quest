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
    //[EnableRateLimiting("fixed")]
    public class TodoController : ControllerBase
    {
        private readonly TodoService _todoService;

        public TodoController(TodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpPost]
        public ActionResult<TodoTask> CreateTask([FromBody] CreateTaskDto dto)
        {
            //read the Id from token
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = _todoService.CreateTask(userId.Value, dto);
            if (!result.Succeeded)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        public ActionResult<List<TodoTask>> GetTasks()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = _todoService.GetTasks(userId.Value);
            if (!result.Succeeded)
            {
                return result.ErrorStatusCode == 404
                    ? NotFound(result.Error)
                    : BadRequest(result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost("{id}/complete")] //POST /api/todo/5/complete
        public ActionResult<CompleteTaskResponseDto> CompleteTask(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var result = _todoService.CompleteTask(userId.Value, id);
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
