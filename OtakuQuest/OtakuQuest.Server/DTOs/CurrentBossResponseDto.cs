using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.DTOs
{
    public class CurrentBossResponseDto
    {
        public Boss Boss { get; set; } = null!;
        public int CurrentHp { get; set; }
        public int TurnNumber { get; set; } = 1;
        public List<CombatSkillDto> PlayerSkills { get; set; } = new();
        public CastingSkillDto? PlayerCasting { get; set; }
        public CastingSkillDto? BossCasting { get; set; }
        public bool PlayerComboReady { get; set; }
        public bool BossComboReady { get; set; }
    }
}
