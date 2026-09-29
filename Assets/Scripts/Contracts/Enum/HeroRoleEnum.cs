namespace MagicSchool.Contracts
{
    // Serialized into HeroDataSO assets as a raw int - always assign explicit values so inserting a
    // new member later can't silently remap what an existing asset's stored int means.
    // Hidden from the player: a design label that decides a hero's stat line, not a trait.
    public enum HeroRoleEnum
    {
        None = 0,   // test units - dummies, minions
        Tank = 1,
        Mage = 2,
        Fighter = 3,
        Marksman = 4,
        Assassin = 5,
    }
}
