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
                .Include(user => user.EquippedAvatar)
                .Include(user => user.CurrentBoss)
                    .ThenInclude(boss => boss!.RewardItem)
                .FirstOrDefaultAsync(user => user.Id == userId);

            if (player == null)
                return ServiceResult<CurrentBossResponseDto>.Failure("Player not found", 404);

            bool assignedNewBoss = false;
            if (player.CurrentBoss == null)
            {
                int nextOrder = player.LastDefeatedBossOrder + 1;
                var nextBoss = await _context.Bosses
                    .Include(boss => boss.RewardItem)
                    .OrderBy(boss => boss.Order)
                    .FirstOrDefaultAsync(boss => boss.Order == nextOrder);

                if (nextBoss == null)
                {
                    nextBoss = await _context.Bosses
                        .Include(boss => boss.RewardItem)
                        .OrderBy(boss => boss.Order)
                        .FirstOrDefaultAsync();

                    if (nextBoss == null)
                    {
                        return ServiceResult<CurrentBossResponseDto>.Failure(
                            "No Boss in this game!",
                            404);
                    }

                    player.LastDefeatedBossOrder = -1;
                }

                player.CurrentBossId = nextBoss.Id;
                player.CurrentBossHp = nextBoss.MaxHp;
                player.CurrentBoss = nextBoss;
                assignedNewBoss = true;
            }

            var combatState = await GetOrCreateCombatStateAsync(player.Id);
            if (assignedNewBoss)
                combatState.Reset();

            await _skillAssignmentService.EnsureAssignmentsAsync(
                player.EquippedAvatar,
                player.CurrentBoss!);

            await _context.SaveChangesAsync();

            return ServiceResult<CurrentBossResponseDto>.Success(
                await BuildCurrentBossResponseAsync(player, combatState));
        }
        // This method is a convenience method for taking a basic attack action. (Maybe delete this?)
        public Task<ServiceResult<CombatResultDto>> AttackBoss(int userId)
        {
            return TakeTurn(userId, new CombatActionDto
            {
                ActionType = CombatActionType.BasicAttack
            });
        }

        public async Task<ServiceResult<CombatResultDto>> TakeTurn(
            int userId,
            CombatActionDto action)
        {
            try
            {
                var player = await LoadCombatPlayerAsync(userId);
                if (player == null)
                    return ServiceResult<CombatResultDto>.Failure("Player not found", 404);

                if (player.CurrentBoss == null)
                    return ServiceResult<CombatResultDto>.Failure("No Boss for you to fight");

                if (player.CurrentHP <= 0)
                {
                    return ServiceResult<CombatResultDto>.Failure(
                        "No more HP for fighting, level up first, or change your character.");
                }

                
                var boss = player.CurrentBoss;
                var combatState = await GetOrCreateCombatStateAsync(player.Id);

                await _skillAssignmentService.EnsureAssignmentsAsync(
                    player.EquippedAvatar,
                    boss);

                // The first action may create the combat state and skill links.
                // Save them before querying through the link tables.
                await _context.SaveChangesAsync();

                var playerSkills = await GetCharacterSkillsAsync(player.EquippedAvatar!.Id);
                var bossSkills = await GetBossSkillsAsync(boss.Id);
                var result = new CombatResultDto();

                string? actionError = ResolvePlayerAction(
                    player,
                    boss,
                    combatState,
                    playerSkills,
                    action,
                    result);

                if (actionError != null)
                    return ServiceResult<CombatResultDto>.Failure(actionError);

                if (player.CurrentBossHp <= 0)
                {
                    await FinishVictoryAsync(player, boss, combatState, result);
                    result.Message = string.Join(" ", result.Events);
                    await _context.SaveChangesAsync();
                    return ServiceResult<CombatResultDto>.Success(result);
                }

                ResolveBossAction(player, boss, combatState, bossSkills, result);

                if (player.CurrentHP <= 0)
                {
                    player.CurrentHP = 0;
                    player.CurrentBossHp = boss.MaxHp;
                    result.PlayerDefeated = true;
                    result.Events.Add(
                        $"{boss.Name} defeated you. The Boss recovered all HP.");
                    combatState.Reset();
                }
                else
                {
                    combatState.TurnNumber++;
                }

                result.Message = string.Join(" ", result.Events);
                await _context.SaveChangesAsync();
                return ServiceResult<CombatResultDto>.Success(result);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<CombatResultDto>.Failure(
                    "This combat turn was already processed. Refresh the battle state.",
                    409);
            }
        }

        private async Task<User?> LoadCombatPlayerAsync(int userId)
        {
            return await _context.Users
                .Include(user => user.EquippedWeapon)
                .Include(user => user.EquippedAvatar)
                .Include(user => user.EquippedBackground)
                .Include(user => user.CurrentBoss)
                    .ThenInclude(boss => boss!.RewardItem)
                .FirstOrDefaultAsync(user => user.Id == userId);
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

        private static string? ResolvePlayerAction(
            User player,
            Boss boss,
            UserCombatState combatState,
            List<Skill> playerSkills,
            CombatActionDto action,
            CombatResultDto result)
        {
            //When player is casting a skill
            if (combatState.PlayerCastingSkillId.HasValue)
            {
                if (action.ActionType != CombatActionType.ContinueCasting)
                    return "You are casting and cannot choose another action.";

                var castingSkill = playerSkills.FirstOrDefault(skill =>
                    skill.Id == combatState.PlayerCastingSkillId.Value);

                if (castingSkill == null)
                {
                    return "The casting skill does not belong to " +
                        "the equipped character.";
                }

                combatState.PlayerCastTurnsRemaining--;

                if (combatState.PlayerCastTurnsRemaining <= 0)
                {
                    ResolvePlayerSkill(player, boss, combatState, castingSkill, result);
                    combatState.PlayerCastingSkillId = null;
                    combatState.PlayerCastTurnsRemaining = 0;
                }
                else
                {
                    result.Events.Add(
                        $"You continue casting {castingSkill.Name}. " +
                        $"{combatState.PlayerCastTurnsRemaining} turn(s) remain.");
                }

                return null;
            }

            if (action.ActionType == CombatActionType.ContinueCasting)
                return "There is no skill being cast.";

            if (action.ActionType == CombatActionType.BasicAttack)
            {
                ResolvePlayerBasicAttack(player, boss, result);
                return null;
            }

            if (action.ActionType != CombatActionType.Skill || !action.SkillId.HasValue)
                return "Choose a valid combat action.";

            var selectedSkill = playerSkills.FirstOrDefault(skill =>
                skill.Id == action.SkillId.Value);

            if (selectedSkill == null)
                return "This skill does not belong to the equipped character.";

            if (player.Level < selectedSkill.UnlockLevel)
                return $"This skill unlocks at level {selectedSkill.UnlockLevel}.";

            if (selectedSkill.CastTurns <= 0)
            {
                ResolvePlayerSkill(player, boss, combatState, selectedSkill, result);
            }
            else
            {
                combatState.PlayerCastingSkillId = selectedSkill.Id;
                combatState.PlayerCastTurnsRemaining = selectedSkill.CastTurns;
                result.Events.Add(
                    $"You started casting {selectedSkill.Name}. " +
                    "Casting cannot be cancelled.");
            }

            return null;
        }

        private static void ResolvePlayerBasicAttack(
            User player,
            Boss boss,
            CombatResultDto result)
        {
            int damage = CalculateDamage(player.TotalSTR, player.TotalINT, boss.DEF);
            player.CurrentBossHp -= damage;
            result.PlayerDamageDealt += damage;
            result.Events.Add(
                $"You used a basic attack and dealt {damage} damage.");
        }

        private static void ResolvePlayerSkill(
            User player,
            Boss boss,
            UserCombatState combatState,
            Skill skill,
            CombatResultDto result)
        {
            decimal multiplier = skill.DamageMultiplier;
            bool comboTriggered = skill.ConsumesCombo && combatState.PlayerComboReady;

            if (comboTriggered)
            {
                multiplier += skill.ComboBonusMultiplier;
                combatState.PlayerComboReady = false;
            }

            int baseDamage = CalculateDamage(player.TotalSTR, player.TotalINT, boss.DEF);
            int damage = Math.Max(
                1,
                (int)Math.Round(
                    baseDamage * multiplier,
                    MidpointRounding.AwayFromZero));

            player.CurrentBossHp -= damage;
            result.PlayerDamageDealt += damage;
            result.Events.Add(comboTriggered
                ? $"{skill.Name} triggered its combo and dealt {damage} damage!"
                : $"{skill.Name} dealt {damage} damage.");

            if (skill.AppliesCombo)
            {
                combatState.PlayerComboReady = true;
                result.Events.Add("Your character's ultimate combo is ready.");
            }
        }

        private static void ResolveBossAction(
            User player,
            Boss boss,
            UserCombatState combatState,
            List<Skill> bossSkills,
            CombatResultDto result)
        {
            if (combatState.BossCastingSkillId.HasValue)
            {
                var castingSkill = bossSkills.FirstOrDefault(skill =>
                    skill.Id == combatState.BossCastingSkillId.Value);

                if (castingSkill == null)
                {
                    combatState.BossCastingSkillId = null;
                    combatState.BossCastTurnsRemaining = 0;
                    ResolveBossBasicAttack(player, boss, result);
                    return;
                }

                combatState.BossCastTurnsRemaining--;

                if (combatState.BossCastTurnsRemaining <= 0)
                {
                    ResolveBossSkill(player, boss, combatState, castingSkill, result);
                    combatState.BossCastingSkillId = null;
                    combatState.BossCastTurnsRemaining = 0;
                }
                else
                {
                    result.Events.Add(
                        $"{boss.Name} continues casting {castingSkill.Name}. " +
                        $"{combatState.BossCastTurnsRemaining} turn(s) remain.");
                }

                return;
            }

            var normalSkill = bossSkills.FirstOrDefault(skill =>
                skill.Slot == SkillSlot.Normal);
            var ultimate = bossSkills.FirstOrDefault(skill =>
                skill.Slot == SkillSlot.Ultimate);
            int roll = Random.Shared.Next(100);

            Skill? selectedSkill = null;
            if (combatState.BossComboReady && ultimate != null && roll < 70)
                selectedSkill = ultimate;
            else if (!combatState.BossComboReady && normalSkill != null && roll < 45)
                selectedSkill = normalSkill;

            if (selectedSkill == null)
            {
                ResolveBossBasicAttack(player, boss, result);
                return;
            }

            if (selectedSkill.CastTurns <= 0)
            {
                ResolveBossSkill(player, boss, combatState, selectedSkill, result);
                return;
            }

            combatState.BossCastingSkillId = selectedSkill.Id;
            combatState.BossCastTurnsRemaining = selectedSkill.CastTurns;
            result.Events.Add(
                $"{boss.Name} started casting {selectedSkill.Name}. " +
                $"It will release after {selectedSkill.CastTurns} turn(s).");
        }

        private static void ResolveBossBasicAttack(
            User player,
            Boss boss,
            CombatResultDto result)
        {
            int damage = CalculateDamage(boss.STR, boss.INT, player.TotalDEF);
            player.CurrentHP -= damage;
            result.BossDamageDealt += damage;
            result.Events.Add(
                $"{boss.Name} used a basic attack and dealt {damage} damage.");
        }

        private static void ResolveBossSkill(
            User player,
            Boss boss,
            UserCombatState combatState,
            Skill skill,
            CombatResultDto result)
        {
            decimal multiplier = skill.DamageMultiplier;
            bool comboTriggered = skill.ConsumesCombo && combatState.BossComboReady;

            if (comboTriggered)
            {
                multiplier += skill.ComboBonusMultiplier;
                combatState.BossComboReady = false;
            }

            int baseDamage = CalculateDamage(boss.STR, boss.INT, player.TotalDEF);
            int damage = Math.Max(
                1,
                (int)Math.Round(
                    baseDamage * multiplier,
                    MidpointRounding.AwayFromZero));

            player.CurrentHP -= damage;
            result.BossDamageDealt += damage;
            result.Events.Add(comboTriggered
                ? $"{boss.Name}'s {skill.Name} triggered its combo and dealt {damage} damage!"
                : $"{boss.Name}'s {skill.Name} dealt {damage} damage.");

            if (skill.AppliesCombo)
            {
                combatState.BossComboReady = true;
                result.Events.Add($"{boss.Name}'s ultimate combo is ready.");
            }
        }

        private async Task FinishVictoryAsync(
            User player,
            Boss boss,
            UserCombatState combatState,
            CombatResultDto result)
        {
            result.BossDefeated = true;
            result.Events.Add(
                $"{boss.Name} defeated! Gained {boss.RewardXP} XP and " +
                $"{boss.RewardCurrency} PrimoGems!");

            player.LastDefeatedBossOrder = boss.Order;
            player.CurrentBossId = null;
            player.CurrentBoss = null;
            player.CurrentBossHp = 0;

            player.AddXp(boss.RewardXP);
            player.Currency += boss.RewardCurrency;

            if (boss.RewardItemId.HasValue)
            {
                bool alreadyOwns = await _context.UserItems.AnyAsync(item =>
                    item.UserId == player.Id &&
                    item.ItemId == boss.RewardItemId.Value);

                if (!alreadyOwns)
                {
                    _context.UserItems.Add(new UserItem
                    {
                        UserId = player.Id,
                        ItemId = boss.RewardItemId.Value
                    });
                    result.RewardItemName = boss.RewardItem?.Name;
                    result.Events.Add($"You received {result.RewardItemName}.");
                }
            }

            combatState.Reset();
        }

        private static int CalculateDamage(
            int strength,
            int intelligence,
            int defence)
        {
            double attackValue = 6d * Math.Sqrt(Math.Max(0, strength))
                + 6d * Math.Sqrt(Math.Max(0, intelligence));
            double defenceValue = 4d * Math.Sqrt(Math.Max(0, defence));

            return Math.Max(
                1,
                (int)Math.Round(
                    attackValue - defenceValue,
                    MidpointRounding.AwayFromZero));
        }
    }
}
