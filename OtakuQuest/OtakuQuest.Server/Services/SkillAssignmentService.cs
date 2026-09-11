using Microsoft.EntityFrameworkCore;
using OtakuQuest.Server.Data;
using OtakuQuest.Server.Models;

namespace OtakuQuest.Server.Services
{
    public class SkillAssignmentService
    {
        private readonly OtakuQuestDbContext _context;

        public SkillAssignmentService(OtakuQuestDbContext context)
        {
            _context = context;
        }

        public async Task EnsureAssignmentsAsync(Item? character, Boss boss)
        {
            if (character?.Type == ItemType.Character)
                await EnsureCharacterAssignmentsAsync(character);

            await EnsureBossAssignmentsAsync(boss);
        }

        private async Task EnsureCharacterAssignmentsAsync(Item character)
        {
            var expectedSkillNames = SkillAssignmentCatalog
                .GetCharacterSkillNames(character.Name);

            if (expectedSkillNames.Count == 0)
                return;

            var expectedSkillIds = await _context.Skills
                .Where(skill => expectedSkillNames.Contains(skill.Name))
                .Select(skill => skill.Id)
                .ToListAsync();

            var existingSkillIds = await _context.CharacterSkills
                .Where(link => link.CharacterItemId == character.Id)
                .Select(link => link.SkillId)
                .ToListAsync();

            foreach (int skillId in expectedSkillIds.Except(existingSkillIds))
            {
                _context.CharacterSkills.Add(new CharacterSkill
                {
                    CharacterItemId = character.Id,
                    SkillId = skillId
                });
            }
        }

        private async Task EnsureBossAssignmentsAsync(Boss boss)
        {
            var expectedSkillNames = SkillAssignmentCatalog.GetBossSkillNames(boss.Name);

            if (expectedSkillNames.Count == 0)
                return;

            var expectedSkillIds = await _context.Skills
                .Where(skill => expectedSkillNames.Contains(skill.Name))
                .Select(skill => skill.Id)
                .ToListAsync();

            var existingSkillIds = await _context.BossSkills
                .Where(link => link.BossId == boss.Id)
                .Select(link => link.SkillId)
                .ToListAsync();

            foreach (int skillId in expectedSkillIds.Except(existingSkillIds))
            {
                _context.BossSkills.Add(new BossSkill
                {
                    BossId = boss.Id,
                    SkillId = skillId
                });
            }
        }
    }
}
