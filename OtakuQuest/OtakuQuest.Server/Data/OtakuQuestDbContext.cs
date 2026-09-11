using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.Data
{
    public class OtakuQuestDbContext :DbContext
    {
        public OtakuQuestDbContext(DbContextOptions<OtakuQuestDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<TodoTask> Tasks { get; set; } = null!;
    
        public DbSet<UserItem> UserItems { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;
        public DbSet<Boss> Bosses { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<CharacterSkill> CharacterSkills { get; set; } = null!;
        public DbSet<BossSkill> BossSkills { get; set; } = null!;
        public DbSet<UserCombatState> UserCombatStates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // It helps the database to understand that a User can have 3 different equipped items, and that they are all optional (nullable foreign keys).
            modelBuilder.Entity<User>()
                .HasOne(u => u.EquippedWeapon)
                .WithMany()
                .HasForeignKey(u => u.EquippedWeaponId)
                .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<User>()
                .HasOne(u => u.EquippedAvatar)
                .WithMany()
                .HasForeignKey(u => u.EquippedAvatarId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .HasOne(u => u.EquippedBackground)
                .WithMany()
                .HasForeignKey(u => u.EquippedBackgroundId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserItem>()
                .HasOne(ui => ui.Item)
                .WithMany()
                .HasForeignKey(ui => ui.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Skill>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<CharacterSkill>()
                .HasKey(cs => new { cs.CharacterItemId, cs.SkillId });

            modelBuilder.Entity<CharacterSkill>()
                .HasOne(cs => cs.CharacterItem)
                .WithMany()
                .HasForeignKey(cs => cs.CharacterItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CharacterSkill>()
                .HasOne(cs => cs.Skill)
                .WithMany()
                .HasForeignKey(cs => cs.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BossSkill>()
                .HasKey(bs => new { bs.BossId, bs.SkillId });

            modelBuilder.Entity<BossSkill>()
                .HasOne(bs => bs.Boss)
                .WithMany()
                .HasForeignKey(bs => bs.BossId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BossSkill>()
                .HasOne(bs => bs.Skill)
                .WithMany()
                .HasForeignKey(bs => bs.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCombatState>()
                .HasOne<User>()
                .WithOne()
                .HasForeignKey<UserCombatState>(state => state.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCombatState>()
                .HasOne<Skill>()
                .WithMany()
                .HasForeignKey(state => state.PlayerCastingSkillId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserCombatState>()
                .HasOne<Skill>()
                .WithMany()
                .HasForeignKey(state => state.BossCastingSkillId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
