namespace MagicSchool.Contracts
{
    /// <summary>
    /// Every stat a hero has. 
    /// </summary>
    // Assets store these ints: never reuse one. 4 was MR (magic resist), removed - DF resists every damage. ArmorPen:
    // % of the target's DF this hero's hits ignore; Lethality: flat DF ignored after ArmorPen (LoL's).
    public enum StatEnum
    {
        None = -1,

        MaxHP = 0,
        ATK = 1,
        DF = 2,
        AP = 3,
        AS = 5,
        Range = 6,
        StartMana = 7,
        MaxMana = 8,

        DamageReduction = 9,
        CritChance = 10,
        Omnivamp = 11,
        ManaPerAttack = 12,
        ManaPerSecond = 13,
        HPPerSecond = 14,
        ArmorPen = 15,
        Lethality = 16,
    }
}
