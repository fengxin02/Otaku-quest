using System.ComponentModel.DataAnnotations;

namespace OtakuQuest.Server.Models
{

    public class TodoTask
    {
        [Required]
        public int Id { get; set; }

        // Foreign Key for User table
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; } = null!;
        [Required]
        public string Title { get; set; } = "Challenge";
        public string Description { get; set; } = string.Empty;
        [Required]
        public TaskType Type { get; set; }
        [Required]
        public DifficultyRank DifficultyRank { get; set; }
        [Required]
        public TaskStatus Status { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public bool IsRepeatable { get; set; } = false;
        [Required]
        public int CompletionCount { get; set; } = 0;
        public DateTime? LastCompletedAt { get; set; }
    }
}
