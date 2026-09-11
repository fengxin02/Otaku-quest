using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.Services
{
    public class BossService
    {
        private readonly OtakuQuestDbContext _context;
        private readonly SkillAssignmentService _skillAssignmentService;

        public BossService(
            OtakuQuestDbContext context,
            SkillAssignmentService skillAssignmentService)
        {
            _context = context;
            _skillAssignmentService = skillAssignmentService;
        }

        public async Task<Boss> CreateBoss(CreateBossDto dto)
        {
            var boss = new Boss
            {
                Order = dto.Order,
                Name = dto.Name,
                Description = dto.Description,
                ImageAsset = dto.ImageAsset,
                MaxHp = dto.MaxHp,
                STR = dto.STR,
                INT = dto.INT,
                DEF = dto.DEF,
                RewardXP = dto.RewardXP,
                RewardCurrency = dto.RewardCurrency,
                RewardItemId = dto.RewardItemId
            };
            _context.Bosses.Add(boss);
            await _context.SaveChangesAsync();
            return boss;
        }

        public async Task<ServiceResult<CurrentBossResponseDto>> GetCurrentBoss(int userId)
        {
            var player = await _context.Users
                .Include(u => u.EquippedAvatar)
                .Include(u => u.CurrentBoss).ThenInclude(b => b.RewardItem)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (player == null)
            {
                return ServiceResult<CurrentBossResponseDto>.Failure("Player not found", 404);
            }
            if (player.CurrentBoss == null)
            {
                int nextOrder = player.LastDefeatedBossOrder + 1;
                var nextBoss = await _context.Bosses
                    .OrderBy(b => b.Order)
                    .Include(b => b.RewardItem)
                    .FirstOrDefaultAsync(b => b.Order == nextOrder);
                if (nextBoss == null)
                {
                    // If no current boss, assign the first one from the database
                    nextBoss = await _context.Bosses.FirstOrDefaultAsync();
                    if (nextBoss == null)
                    {
                        return ServiceResult<CurrentBossResponseDto>.Failure("No Boss in this game!", 404);
                    }
                    player.LastDefeatedBossOrder = -1; // Reset to the first boss order
                }

                player.CurrentBossId = nextBoss.Id;
                player.CurrentBossHp = nextBoss.MaxHp;
                player.CurrentBoss = nextBoss;
            }

            var combatState = await GetOrCreateCombatStateAsync(player.Id);
            await _skillAssignmentService.EnsureAssignmentsAsync(
                player.EquippedAvatar,
                player.CurrentBoss);

            await _context.SaveChangesAsync();

            return ServiceResult<CurrentBossResponseDto>.Success(
                await BuildCurrentBossResponseAsync(player, combatState));
        }

        private async Task<UserCombatState> GetOrCreateCombatStateAsync(int userId)
        {
            var combatState = await _context.UserCombatStates
                .FirstOrDefaultAsync(state => state.UserId == userId);

            if (combatState != null)
                return combatState;

            combatState = new UserCombatState { UserId = userId };
            _context.UserCombatStates.Add(combatState);
            return combatState;
        }

        private async Task<CurrentBossResponseDto> BuildCurrentBossResponseAsync(
            User player,
            UserCombatState combatState)
        {
            var playerSkills = player.EquippedAvatarId.HasValue
                ? await GetCharacterSkillsAsync(player.EquippedAvatarId.Value)
                : new List<Skill>();

            var bossSkills = await GetBossSkillsAsync(player.CurrentBoss!.Id);

            var playerCastingSkill = combatState.PlayerCastingSkillId.HasValue
                ? playerSkills.FirstOrDefault(skill =>
                    skill.Id == combatState.PlayerCastingSkillId.Value)
                : null;

            var bossCastingSkill = combatState.BossCastingSkillId.HasValue
                ? bossSkills.FirstOrDefault(skill =>
                    skill.Id == combatState.BossCastingSkillId.Value)
                : null;

            return new CurrentBossResponseDto
            {
                Boss = player.CurrentBoss,
                CurrentHp = Math.Max(0, player.CurrentBossHp),
                TurnNumber = combatState.TurnNumber,
                PlayerComboReady = combatState.PlayerComboReady,
                BossComboReady = combatState.BossComboReady,
                PlayerSkills = playerSkills.Select(skill => new CombatSkillDto
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Slot = skill.Slot,
                    CastTurns = skill.CastTurns,
                    UnlockLevel = skill.UnlockLevel,
                    DamageMultiplier = skill.DamageMultiplier,
                    ComboBonusMultiplier = skill.ComboBonusMultiplier,
                    IsUnlocked = player.Level >= skill.UnlockLevel
                }).ToList(),
                PlayerCasting = playerCastingSkill == null
                    ? null
                    : new CastingSkillDto
                    {
                        SkillId = playerCastingSkill.Id,
                        SkillName = playerCastingSkill.Name,
                        TurnsRemaining = combatState.PlayerCastTurnsRemaining
                    },
                BossCasting = bossCastingSkill == null
                    ? null
                    : new CastingSkillDto
                    {
                        SkillId = bossCastingSkill.Id,
                        SkillName = bossCastingSkill.Name,
                        TurnsRemaining = combatState.BossCastTurnsRemaining
                    }
            };
        }

        private async Task<List<Skill>> GetCharacterSkillsAsync(int characterItemId)
        {
            return await _context.CharacterSkills
                .Where(link => link.CharacterItemId == characterItemId)
                .Select(link => link.Skill)
                .OrderBy(skill => skill.Slot)
                .ToListAsync();
        }

        private async Task<List<Skill>> GetBossSkillsAsync(int bossId)
        {
            return await _context.BossSkills
                .Where(link => link.BossId == bossId)
                .Select(link => link.Skill)
                .OrderBy(skill => skill.Slot)
                .ToListAsync();
        }

        public async Task<ServiceResult<CombatResultDto>> AttackBoss(int userId)
        {
            var player = await _context.Users
                .Include(u => u.EquippedWeapon)
                .Include(u => u.EquippedAvatar)
                .Include(u => u.EquippedBackground)
                .Include(u => u.CurrentBoss).ThenInclude(b => b.RewardItem)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (player == null)
            {
                return ServiceResult<CombatResultDto>.Failure("Player not found");
            }
            if (player.CurrentBoss == null)
            {
                return ServiceResult<CombatResultDto>.Failure("No Boss for you to fight");
            }

            if (player.CurrentHP <= 0)
                return ServiceResult<CombatResultDto>.Failure("No more HP for fighting, level up first!");

            var boss = player.CurrentBoss;
            var result = new CombatResultDto();

            // --- Player Attack ---
            int playerDamage = CalculateDamage(player.TotalSTR, player.TotalINT, boss.DEF);
            player.CurrentBossHp -= playerDamage;
            result.PlayerDamageDealt = playerDamage;

            // Check if Boss is defeated
            if (player.CurrentBossHp <= 0)
            {
                result.BossDefeated = true;
                result.BossDamageDealt = 0;
                result.Message = $"{boss.Name} defeted! Gained {boss.RewardXP} XP and {boss.RewardCurrency} PrimoGems!";

                //Reward the player
                player.AddXp(boss.RewardXP);
                player.Currency += boss.RewardCurrency;

                if (boss.RewardItemId.HasValue)
                {
                    bool alreadyOwns = await _context.UserItems
                        .AnyAsync(ui => ui.UserId == player.Id && ui.ItemId == boss.RewardItemId);

                    if (!alreadyOwns)
                    {
                        _context.UserItems.Add(new UserItem { UserId = player.Id, ItemId = boss.RewardItemId.Value });
                        result.RewardItemName = boss.RewardItem?.Name;
                        result.Message += $" Congratulation you got a new Item: {result.RewardItemName}!";
                    }
                }
                player.LastDefeatedBossOrder = boss.Order;
                player.CurrentBossId = null;

                await _context.SaveChangesAsync();
                return ServiceResult<CombatResultDto>.Success(result);
            }

            // --- BOSS Attack ---
            int bossDamage = CalculateDamage(boss.STR, boss.INT, player.TotalDEF);
            player.CurrentHP -= bossDamage;
            result.BossDamageDealt = bossDamage;

            // Check if player did not suvive the boss attack
            if (player.CurrentHP <= 0)
            {
                player.CurrentHP = 0;
                result.PlayerDefeated = true;
                result.Message = $"Lost! {boss.Name} killed you. Try again with more powerfull items!";

                // Reset the boss HP for the next fight
                player.CurrentBossHp = boss.MaxHp;
            }
            else
            {
                result.Message = $"The fight is going on, you dealt {playerDamage} damage, boss deealt {bossDamage} damage.";
            }

            await _context.SaveChangesAsync();
            return ServiceResult<CombatResultDto>.Success(result);
        }

        private static int CalculateDamage(int strength, int intelligence, int defence)
        {
            double attackValue = 6d * Math.Sqrt(Math.Max(0, strength))
                + 6d * Math.Sqrt(Math.Max(0, intelligence));
            double defenceValue = 4d * Math.Sqrt(Math.Max(0, defence));

            return Math.Max(
                1,
                (int)Math.Round(attackValue - defenceValue, MidpointRounding.AwayFromZero));
        }
    }
}
