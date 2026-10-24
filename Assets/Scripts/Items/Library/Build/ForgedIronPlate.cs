using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Forged Iron Plate: +50 DF. Blacksmith forges it mid-fight for a tank, or for herself; never in the shop.
    internal static class ForgedIronPlate
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.DF, 50f));
        }
    }
}
