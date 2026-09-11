namespace OtakuQuest.Server.DTOs
{
    public class CastingSkillDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int TurnsRemaining { get; set; }
    }
}
