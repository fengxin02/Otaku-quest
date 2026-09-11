namespace OtakuQuest.Server.Models
{
    public class CharacterSkill
    {
        public int CharacterItemId { get; set; }
        public Item CharacterItem { get; set; } = null!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;
    }
}
