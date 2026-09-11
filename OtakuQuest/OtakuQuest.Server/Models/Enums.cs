namespace OtakuQuest.Server.Models
{
    public enum TaskType
    {
        Study,
        Workout,
        Hobby,
        Social,
        Health

    }
    public enum DifficultyRank
    {
        E,D,C, B, A, S
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public enum CombatActionType
    {
        BasicAttack = 0,
        Skill = 1,
        ContinueCasting = 2
    }

    public enum SkillSlot
    {
        Normal = 0,
        Ultimate = 1
    }

}
