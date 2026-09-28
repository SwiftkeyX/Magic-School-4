namespace MagicSchool.Contracts
{
    // Serialized into SkillSO assets as a raw int - always assign explicit values so inserting a
    // new member later can't silently remap what an existing asset's stored int means.
    public enum ModifierEnum
    {
        // ======================================= Buff =======================================
        BonusHP = 0,
        DamageReduction = 2,
        AS = 5,
        ATK = 6,
        DF = 7,
        AP = 8,
        Omnivamp = 9,
        Range = 14,
        StartMana = 15,

        // ======================================= Debuff =======================================
        DefendShred = 13,

        // ======================================= Status =======================================
        Stun = 3,
        Wound = 4,
        Transformed = 10,
        ManaBlocked = 11,
        AutoAttackWasReplaced = 12,
    }
}
