using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.DTOs
{
    public class CombatSkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SkillSlot Slot { get; set; }
        public int CastTurns { get; set; }
        public int UnlockLevel { get; set; }
        public decimal DamageMultiplier { get; set; }
        public decimal ComboBonusMultiplier { get; set; }
        public bool IsUnlocked { get; set; }
    }
}
