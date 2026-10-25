using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Reinforced Iron Plate: +40 DF. Blacksmith forges it mid-fight (her forge table); never in the shop.
    internal static class ReinforcedIronPlate
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.DF, 40f));
        }
    }
}
