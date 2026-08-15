using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;

namespace OtakuQuest.Server.Services
{
    public class PlayerProfileService
    {
        private readonly OtakuQuestDbContext _context;

        public PlayerProfileService(OtakuQuestDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<PlayerStatsDto>> GetStats(int userId)
        {
            var player = await _context.Users
                .Include(u => u.EquippedAvatar)
                .Include(u => u.EquippedBackground)
                .Include(u => u.EquippedWeapon)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (player == null)
            {
                return ServiceResult<PlayerStatsDto>.Failure("Player not found", 404);
            }

            var statsDto = new PlayerStatsDto
            {
                Username = player.Username,
                Level = player.Level,
                XP = player.XP,
                Currency = player.Currency,
                STR = player.TotalSTR,
                INT = player.TotalINT,
                DEF = player.TotalDEF,
                CurrentHP = player.CurrentHP,
                MaxHP = player.TotalMaxHP,
                AvatarImage = player.EquippedAvatar?.ImageAsset,
                BackgroundImage = player.EquippedBackground?.ImageAsset,
                WeaponImage = player.EquippedWeapon?.ImageAsset,
                WeaponName = player.EquippedWeapon?.Name
            };

            return ServiceResult<PlayerStatsDto>.Success(statsDto);
        }
    }
}
