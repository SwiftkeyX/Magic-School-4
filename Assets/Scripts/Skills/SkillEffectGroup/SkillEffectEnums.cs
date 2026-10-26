namespace MagicSchool.Skills
{
    // ================================================ Recipient ================================================
    // Assets store these enums as raw ints: values are explicit so a new member never remaps a stored one.
    // ModifierEnum lives in Contracts instead, so Hero can name a buff without the skill system.
    public enum EffectRecipientEnum
    {
        Self = 0,
        EnemiesInArea = 1,
        SameToAimTarget = 2,
        EnemiesInPath = 3,
        AlliesInPath = 4,
        AlliesInArea = 5,      
    }

    // skill condition can either ask Caster or each Recipients
    public enum ConditionSubjectEnum
    {
        Caster = 0,
        Recipient = 1,
    }

    // what skill condition return.
    public enum ConditionResultEnum
    {
        NoConditionFound,
        ConditionIsMet,
        ConditionIsNotMet,
    }
}
