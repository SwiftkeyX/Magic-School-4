namespace MagicSchool.Contracts
{
    // The items a skill can forge mid-fight (See IItemWearer). Each one is an item asset, named on the forging hero's
    // HeroDataSO in this order, so neither Skills nor Combat has to name an item.
    // e.g.     Blacksmith gives a Whetstone to a fighter, an Archmage Tome to a mage, an Iron Plate to a tank
    public enum ForgedItemEnum
    {
        Whetstone = 0,
        ArchmageTome = 1,
        ForgedIronPlate = 2,        // +50 DF
        IronPlate = 3,              // +20 DF, the shop's
        MasterworkIronPlate = 4,    // +100 DF
    }
}
