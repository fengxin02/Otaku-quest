using System.ComponentModel.DataAnnotations;

namespace OtakuQuest.Server.Models
{
    public class UserCombatState
    {
        [Key]
        public int UserId { get; set; }

        public int TurnNumber { get; set; } = 1;

        public int? PlayerCastingSkillId { get; set; }
        public int PlayerCastTurnsRemaining { get; set; }
        public bool PlayerComboReady { get; set; }

        public int? BossCastingSkillId { get; set; }
        public int BossCastTurnsRemaining { get; set; }
        public bool BossComboReady { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
