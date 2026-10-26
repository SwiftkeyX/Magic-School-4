namespace MagicSchool.Contracts
{
    // IItemWearer answers: what does this unit wear, and can a skill hand it an item mid-fight?
    // A forged item is a real item, worn on top of the one slot, for the rest of the fight only.
    // e.g.     Blacksmith forges an item for the nearest ally wearing nothing
    // ReceiveForgedItem: the item the forger's table lists for that role and tier, e.g. a tank at tier 2 gets the +40
    // DF plate (See IForger, HeroDataSO.ForgedItem).
    public interface IItemWearer
    {
        int WornItemCount { get; }
        HeroRoleEnum Role { get; }

        void ReceiveForgedItem(HeroRoleEnum forRole, int tier, ICombatant forger);
    }

    // IForger answers: does my forge table have anything for this role? The items themselves are only named on the
    // forger's HeroDataSO, so a skill never names an item.
    public interface IForger
    {
        bool CanForgeFor(HeroRoleEnum role);
    }
}
