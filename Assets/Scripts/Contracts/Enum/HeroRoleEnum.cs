namespace MagicSchool.Contracts
{
    // Role of the hero are hidden from the player. It was here mainly to help design the system.
    // 1) Hero in the same role have the same stat
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
