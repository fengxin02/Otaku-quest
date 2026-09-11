namespace OtakuQuest.Server.Models
{
    public class BossSkill
    {
        public int BossId { get; set; }
        public Boss Boss { get; set; } = null!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;
    }
}
