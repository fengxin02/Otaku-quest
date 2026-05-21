using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtakuQuest.Server.Models
{
    public class User
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Username { get; set; } = "User";
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // RPG Statistics
        [Required]
        public int Level { get; set; } = 1;
        [Required]
        public int XP { get; set; } = 0;
        [Required]
        public int Currency { get; set; } = 0;
        [Required]
        public int MaxHP { get; set; } = 100;
        [Required]
        public int CurrentHP { get; set; } = 100;
        [Required]
        public int STR { get; set; } = 1;
        [Required]
        public int INT { get; set; } = 1;
        [Required]
        public int DEF { get; set; } = 1;

        // Foreign keys for the items
        public int? AvatarItemId { get; set; }
        public int? BackgroundItemId { get; set; }

        [Required]
        public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();

        //Foreign Keys
        public int? EquippedWeaponId { get; set; }
        public Item? EquippedWeapon { get; set; }

        public int? EquippedAvatarId { get; set; }

        public Item? EquippedAvatar { get; set; }

        public int? EquippedBackgroundId { get; set; }
        public Item? EquippedBackground { get; set; }

        // Every item the user has ever acquired, including equipped and unequipped items
        [Required]
        public ICollection<UserItem> Inventory { get; set; } = new List<UserItem>();

        //Boss 
        public int? CurrentBossId { get; set; }
        public Boss? CurrentBoss { get; set; }

        public int CurrentBossHp { get; set; }
        [Required]
        public int LastDefeatedBossOrder { get; set; } = -1;

        [NotMapped]
        public int TotalSTR
        {
            get
            {
                int bonus = (EquippedWeapon?.StrBonus ?? 0) +
                    (EquippedAvatar?.StrBonus ?? 0) +
                    (EquippedBackground?.StrBonus ?? 0);
                float mult = (EquippedWeapon?.StrMultiplier ?? 1f) *
                    (EquippedAvatar?.StrMultiplier ?? 1f) *
                    (EquippedBackground?.StrMultiplier ?? 1f);
                return (int)((STR + bonus) * mult);
            }
        }

        [NotMapped]
        public int TotalINT
        {
            get
            {
                int bonus = (EquippedWeapon?.IntBonus ?? 0) +
                    (EquippedAvatar?.IntBonus ?? 0) +
                    (EquippedBackground?.IntBonus ?? 0);
                float mult = (EquippedWeapon?.IntMultiplier ?? 1f) *
                    (EquippedAvatar?.IntMultiplier ?? 1f) *
                    (EquippedBackground?.IntMultiplier ?? 1f);
                return (int)((INT + bonus) * mult);
            }
        }
        [NotMapped]
        public int TotalDEF
        {
            get
            {
                int bonus = (EquippedWeapon?.DefBonus ?? 0) +
                    (EquippedAvatar?.DefBonus ?? 0) +
                    (EquippedBackground?.DefBonus ?? 0);
                float mult = (EquippedWeapon?.DefMultiplier ?? 1f) *
                    (EquippedAvatar?.DefMultiplier ?? 1f) *
                    (EquippedBackground?.DefMultiplier ?? 1f);
                return (int)((DEF + bonus) * mult);
            }
        }
        [NotMapped]
        public int TotalMaxHP
        {
            get
            {
                int bonus = (EquippedWeapon?.HpBonus ?? 0) +
                    (EquippedAvatar?.HpBonus ?? 0) +
                    (EquippedBackground?.HpBonus ?? 0);
                float mult = (EquippedWeapon?.HpMultiplier ?? 1f)
                    * (EquippedAvatar?.HpMultiplier ?? 1f) *
                    (EquippedBackground?.HpMultiplier ?? 1f);
                return (int)((MaxHP + bonus) * mult);
            }
        }

        public void AddXp(int xpAmount)
        {
            XP += xpAmount;

          
            int xpNeeded =  Level * 100;

            while (XP >= xpNeeded)
            {
                XP -= xpNeeded; 
                Level++;

                MaxHP += 20;
                STR += 2;
                INT += 2;
                DEF += 1;
                Currency += 100;
                CurrentHP = TotalMaxHP;

                xpNeeded = Level * 100;

                if (CurrentBoss != null)
                {
                    CurrentBossHp = CurrentBoss.MaxHp;
                }
            }
        }
    }
}
