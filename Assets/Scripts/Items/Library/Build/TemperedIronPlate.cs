using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Tempered Iron Plate: +30 DF. Blacksmith forges it mid-fight (her forge table); never in the shop.
    internal static class TemperedIronPlate
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.DF, 30f));
        }
    }
}
