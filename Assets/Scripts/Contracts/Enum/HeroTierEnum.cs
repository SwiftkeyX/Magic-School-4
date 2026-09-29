namespace MagicSchool.Contracts
{
    // Serialized into HeroDataSO assets as a raw int - always assign explicit values so inserting a
    // new member later can't silently remap what an existing asset's stored int means.
    // Replaces gold cost: heroes of every tier have similar stats, a higher tier has a stronger skill.
    public enum HeroTierEnum
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
    }
}
