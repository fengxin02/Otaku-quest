using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtakuQuest.Server.Models
{
    public class Skill
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public SkillSlot Slot { get; set; }

        [Required]
        public int CastTurns { get; set; }

        [Required]
        public int UnlockLevel { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DamageMultiplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal ComboBonusMultiplier { get; set; }

        [Required]
        public bool AppliesCombo { get; set; }

        [Required]
        public bool ConsumesCombo { get; set; }
    }
}
