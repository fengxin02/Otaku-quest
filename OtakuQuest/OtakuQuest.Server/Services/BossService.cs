using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.Services
{
    public class BossService
    {
        private readonly OtakuQuestDbContext _context;

        public BossService(OtakuQuestDbContext context)
        {
            _context = context;
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
                await _context.SaveChangesAsync();

                return ServiceResult<CurrentBossResponseDto>.Success(new CurrentBossResponseDto { Boss = nextBoss, CurrentHp = player.CurrentBossHp });
            }

            return ServiceResult<CurrentBossResponseDto>.Success(new CurrentBossResponseDto { Boss = player.CurrentBoss, CurrentHp = player.CurrentBossHp });
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
            int playerDamage = Math.Max(1, player.TotalSTR + (player.TotalINT * 2) - boss.DEF);
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
            int bossDamage = Math.Max(1, boss.STR + (boss.INT * 2) - player.TotalDEF);
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
    }
}
