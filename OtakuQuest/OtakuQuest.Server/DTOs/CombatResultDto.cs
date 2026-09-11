using System.ComponentModel.DataAnnotations;

namespace OtakuQuest.Server.DTOs
{
    public class CombatResultDto
    {
        [Required]
        public int PlayerDamageDealt { get; set; }
        [Required]
        public int BossDamageDealt { get; set; }
        [Required]
        public bool BossDefeated { get; set; }
        [Required]
        public bool PlayerDefeated { get; set; }

        public string? RewardItemName { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Events { get; set; } = new();
    }
}
