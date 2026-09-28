namespace MagicSchool.Contracts
{
    /// <summary>
    /// Every stat a hero has. Stat keys its base values by this instead of one field per stat,
    /// so adding a stat is: one member here + one line in Stat's constructor + one line in
    /// HeroDataSO - instead of a field, a base getter, a modified getter and a ModifiedX()
    /// method spread across two classes.
    /// </summary>
    // Not serialized into any asset today (HeroDataSO still has one field per stat), so the
    // explicit values are only here to keep it that way if it ever does get serialized.
    public enum StatEnum
    {
        None = -1,

        MaxHP = 0,
        ATK = 1,
        DF = 2,
        AP = 3,
        MR = 4,
        AS = 5,
        Range = 6,
        StartMana = 7,
        MaxMana = 8,

        // ====================== Extension ======================
        DamageReduction = 9,
    }
}
