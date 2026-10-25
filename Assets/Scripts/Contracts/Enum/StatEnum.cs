namespace MagicSchool.Contracts
{
    /// <summary>
    /// Every stat a hero has. 
    /// </summary>
    public enum StatEnum
    {
        None = -1,

        MaxHP = 0,
        ATK = 1,
        DF = 2,
        AP = 3,
        // 4 was MR (magic resist), removed: DF resists every damage. Do not reuse 4.
        AS = 5,
        Range = 6,
        StartMana = 7,
        MaxMana = 8,

        // ====================== Extension ======================
        DamageReduction = 9,
        CritChance = 10,
        Omnivamp = 11,
        ManaPerAttack = 12,
        ManaPerSecond = 13,
        HPPerSecond = 14,
        ArmorPen = 15,          // % of the target's DF this hero's hits ignore (LoL's armor penetration)
        Lethality = 16,         // flat DF this hero's hits ignore, after ArmorPen (LoL's lethality)
    }
}
