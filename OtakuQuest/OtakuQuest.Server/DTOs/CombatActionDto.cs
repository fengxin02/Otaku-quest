using System.ComponentModel.DataAnnotations;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.DTOs
{
    public class CombatActionDto
    {
        [Required]
        public CombatActionType ActionType { get; set; }

        public int? SkillId { get; set; }
    }
}
