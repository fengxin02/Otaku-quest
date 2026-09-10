
using OtakuQuest.Server.Models;
using System.ComponentModel.DataAnnotations;
namespace OtakuQuest.Server.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        public string Title { get; set; } = "Challenge";

        public string Description { get; set; } = string.Empty;
        [Required]
        public TaskType Type { get; set; }
        [Required]
        public DifficultyRank DifficultyRank { get; set; }
        [Required]
        public bool IsRepeatable { get; set; } = false;
    }
}
