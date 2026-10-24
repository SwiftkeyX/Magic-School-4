namespace MagicSchool.Contracts
{
    // IItemWearer answers: what does this unit wear, and can a skill hand it an item mid-fight?
    // A forged item is a real item, worn on top of the one slot, for the rest of the fight only.
    // e.g.     Blacksmith forges an item for every ally wearing nothing
    public interface IItemWearer
    {
        int WornItemCount { get; }
        HeroRoleEnum Role { get; }

        // forged by `forger`, whose HeroDataSO names the item asset (See HeroDataSO.ForgedItem)
        void ReceiveForgedItem(ForgedItemEnum item, ICombatant forger);
    }
}
