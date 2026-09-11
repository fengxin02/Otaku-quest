namespace OtakuQuest.Server.Data
{
    public static class SkillAssignmentCatalog
    {
        public static IReadOnlyList<string> GetCharacterSkillNames(string characterName)
        {
            return CharacterSkills.TryGetValue(characterName, out var skillNames)
                ? skillNames
                : Array.Empty<string>();
        }

        public static IReadOnlyList<string> GetBossSkillNames(string bossName)
        {
            return BossSkills.TryGetValue(bossName, out var skillNames)
                ? skillNames
                : Array.Empty<string>();
        }

        private static readonly IReadOnlyDictionary<string, string[]> CharacterSkills =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default Avatar"] = new[] { "Focused Strike", "Heroic Finish" },
                ["Sakura"] = new[] { "Petal Brand", "Thousand Petal Bloom" },
                ["Togawa Sakiko"] = new[] { "Dissonant Note", "Final Movement" },
                ["Luluka"] = new[] { "Predator's Trace", "Crimson Hunt" },
                ["Carlotta"] = new[] { "Crystal Target", "Winter's Verdict" },
                ["Koro Sensei"] = new[] { "Teacher's Mark", "Mach 20 Lesson" }
            };

        private static readonly IReadOnlyDictionary<string, string[]> BossSkills =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bowser"] = new[] { "Burning Brand", "Koopa Inferno" },
                ["Koro Sensei"] = new[] { "Assassination Lesson", "Final Exam" }
            };
    }
}
