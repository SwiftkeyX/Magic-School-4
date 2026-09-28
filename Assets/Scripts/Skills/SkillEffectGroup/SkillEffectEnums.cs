namespace MagicSchool.Skills
{
    /// <summary>
    /// Skil use A LOT OF enum. We group all of them here for readability.
    /// The two that Hero also speaks - TriggerEnum and ModifierEnum - live in Contracts/ instead,
    /// so Hero doesn't have to depend on the whole skill system just to name a buff or a trigger.
    /// </summary>

    // These enums are serialized into SkillSO assets as raw ints - always assign explicit values so
    // inserting a new member later can't silently remap what an existing asset's stored int means.

    // ================================================ Recipient ================================================
    public enum EffectRecipientEnum
    {
        Self = 0,
        EnemiesInArea = 1,
        SameToAimTarget = 2,
        EnemiesInPath = 3,
        AlliesInPath = 4,
    }

    // skill condition can either ask Caster or each Recipients
    public enum ConditionSubjectEnum
    {
        Caster = 0,     // e.g. Is Caster transformed?
        Recipient = 1,  // e.g. Is this recipient wounded?
    }

    // what skill condition return.
    public enum ConditionResultEnum
    {
        NoConditionFound,
        ConditionIsMet,
        ConditionIsNotMet,
    }
}
