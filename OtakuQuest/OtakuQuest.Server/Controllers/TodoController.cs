using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;
using System.Security.Claims;

namespace OtakuQuest.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    //[EnableRateLimiting("fixed")]
    public class TodoController : ControllerBase
    {
        private readonly OtakuQuestDbContext _context;

        public TodoController(OtakuQuestDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public ActionResult<TodoTask> CreateTask([FromBody] CreateTaskDto dto)
        {
            //read thet Id from token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null)
            {
                return Unauthorized("User ID not found in token");
            }
            var userId = int.Parse(userIdString);

            var incompleteCount = _context.Tasks
                .Count(t => t.UserId == userId && t.Status != Models.TaskStatus.Completed);
            if (incompleteCount >= 100)
            {
                return BadRequest("You have reached the maximum of 100 active challenges. Complete some first!");
            }

            var newTask = new Models.TodoTask
            {
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                DifficultyRank = dto.DifficultyRank,
                Status = Models.TaskStatus.InProgress,
                CreatedAt = DateTime.UtcNow

            };
            _context.Tasks.Add(newTask);
            _context.SaveChanges();
            return Ok(newTask);
        }

        [HttpGet]
        public ActionResult<List<TodoTask>> GetTasks()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null)
            {
                return Unauthorized("User ID not found in token");
            }
            var userId = int.Parse(userIdString);

            var tasks = _context.Tasks.Where(t => t.UserId == userId).ToList();
            return Ok(tasks);
        }

        [HttpPost("{id}/complete")] //POST /api/todo/5/complete
        public ActionResult<CompleteTaskResponseDto> CompleteTask(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null)
            {
                return Unauthorized("User ID not found in token");
            }
            var userId = int.Parse(userIdString);
            var task = _context.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);
            if (task == null)
            {
                return NotFound("Task not found");
            }
            var player = _context.Users
                .Include(u => u.EquippedWeapon)
                .Include(u => u.EquippedAvatar)
                .Include(u => u.EquippedBackground)
                .Include(u => u.CurrentBoss)
                .FirstOrDefault(u => u.Id == userId);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            if (task.Status == Models.TaskStatus.Completed)
            {
                return BadRequest("This Challenge is already completed");
            }
            task.Status = Models.TaskStatus.Completed;

            // Reward the player based on the task's difficulty
            int xp = 0;
            int currency = 0;

            switch (task.DifficultyRank)
            {
                case Models.DifficultyRank.E:
                    xp = 10;
                    currency = 5;
                    break;
                case Models.DifficultyRank.D:
                    xp = 20;
                    currency = 15;
                    break;
                case Models.DifficultyRank.C:
                    xp = 30;
                    currency = 30;
                    break;
                case Models.DifficultyRank.B:
                    xp = 45;
                    currency = 60;
                    break;
                case Models.DifficultyRank.A:
                    xp = 50;
                    currency = 200;
                    break;
                case Models.DifficultyRank.S:
                    xp = 100;
                    currency = 300;
                    break;
            }
            int intteligence = 0;
            int strength = 0;
            int defence = 0;
            //str, int, def
            switch (task.Type)
            {
                case TaskType.Health: 
                    defence = 10; 
                    break;
                case TaskType.Workout: 
                    strength = 5; 
                    break;
                case TaskType.Hobby: 
                     defence = 5; 
                    break;
                case TaskType.Social:
                    intteligence = 8;
                    break;
                case TaskType.Study:
                    intteligence = 15;
                    break;
            }
            
            xp = (int) xp * player.Level/2;
            
            player.AddXp(xp);
            player.Currency += currency;
            player.STR += strength;
            player.INT += intteligence;
            player.DEF += defence;

         

            _context.SaveChanges();
            var responseDto = new CompleteTaskResponseDto
            {
                Message ="Challenge completed successfully!",
                XPReward = xp,
                CurrencyReward = currency,
                StrengthReward = strength,
                IntelligenceReward = intteligence,
                DefenceReward = defence,
                NewLevel = player.Level,
                CurrentXP = player.XP,
            };

            return Ok(responseDto);
        }
    }
}
