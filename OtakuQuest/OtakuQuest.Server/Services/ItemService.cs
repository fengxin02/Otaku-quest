using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.DTOs;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.Services
{
    public class ItemService
    {
        private readonly OtakuQuestDbContext _context;

        public ItemService(OtakuQuestDbContext context)
        {
            _context = context;
        }

        public async Task<List<Item>> GetShopItems()
        {
            var items = await _context.Items
                .Where(i => i.IsPurchasable == true).ToListAsync();
            return items;
        }

        public async Task<ServiceResult<Item>> BuyItem(int userId, BuyItemDto dto)
        {
            var player = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (player == null)
            {
                return ServiceResult<Item>.Failure("Player not found", 404);
            }

            var itemToBuy = await _context.Items.FirstOrDefaultAsync(i => i.Id == dto.ItemId);
            if (itemToBuy == null)
            {
                return ServiceResult<Item>.Failure("Item not found", 404);
            }
            bool alreadyOwns = await _context.UserItems
                .AnyAsync(ui => ui.UserId == userId && ui.ItemId == dto.ItemId);
            if (alreadyOwns)
            {
                return ServiceResult<Item>.Failure("You already own this item!");
            }

            if (player.Currency < itemToBuy.Price)
            {
                return ServiceResult<Item>.Failure("Not enough currency!");
            }

            player.Currency -= itemToBuy.Price;

            _context.UserItems.Add(new UserItem
            {
                UserId = player.Id,
                ItemId = itemToBuy.Id
            });

            await _context.SaveChangesAsync();
            return ServiceResult<Item>.Success(itemToBuy);
        }

        public async Task<ServiceResult<Item>> EquipItem(int userId, EquipItemDto dto)
        {
            var player = await _context.Users
                .Include(u => u.EquippedWeapon)
                .Include(u => u.EquippedAvatar)
                .Include(u => u.EquippedBackground)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (player == null)
            {
                return ServiceResult<Item>.Failure("Player not found", 404);
            }

            var itemExist = await _context.Items.
                FirstOrDefaultAsync(i => i.Id == dto.ItemId);
            if (itemExist == null)
            {
                return ServiceResult<Item>.Failure("Item not found", 404);
            }
            var userItem = await _context.UserItems
                .Include(ui => ui.Item)
                .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.ItemId == dto.ItemId);
            if (userItem == null)
            {
                return ServiceResult<Item>.Failure("You don't own this item!");
            }
            var itemToEquip = userItem.Item;

            double hpPercentBefore = (double)player.CurrentHP / player.TotalMaxHP;
            switch (itemToEquip.Type)
            {
                case ItemType.Weapon:
                    player.EquippedWeaponId = itemToEquip.Id;
                    player.EquippedWeapon = itemToEquip;
                    break;
                case ItemType.Character:
                    player.EquippedAvatarId = itemToEquip.Id;
                    player.EquippedAvatar = itemToEquip;
                    break;
                case ItemType.Background:
                    player.EquippedBackgroundId = itemToEquip.Id;
                    player.EquippedBackground = itemToEquip;
                    break;
                default:
                    return ServiceResult<Item>.Failure("Invalid item type!");
            }

            player.CurrentHP = (int)(player.TotalMaxHP * hpPercentBefore);
            await _context.SaveChangesAsync();
            return ServiceResult<Item>.Success(itemToEquip);
        }

        public async Task<ServiceResult<List<Item>>> GetMyInventory(int userId)
        {
            var player = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (player == null)
            {
                return ServiceResult<List<Item>>.Failure("Player not found", 404);
            }

            var myItems = await _context.UserItems
                .Where(ui => ui.UserId == userId)
                .Select(ui => ui.Item)
                .ToListAsync();

            return ServiceResult<List<Item>>.Success(myItems);
        }

        public async Task<Item> CreateItem(CreateItemDto dto)
        {
            var newItem = new Item
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Price = dto.Price,
                ImageAsset = dto.ImageAsset,
                HpBonus = dto.HpBonus,
                StrBonus = dto.StrBonus,
                IntBonus = dto.IntBonus,
                DefBonus = dto.DefBonus,
                HpMultiplier = dto.HpMultiplier,
                StrMultiplier = dto.StrMultiplier,
                IntMultiplier = dto.IntMultiplier,
                DefMultiplier = dto.DefMultiplier,
                IsPurchasable = dto.IsPurchasable
            };

            _context.Items.Add(newItem);
            await _context.SaveChangesAsync();

            return newItem;
        }
    }
}
