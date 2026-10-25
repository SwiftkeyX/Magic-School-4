using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Masterwork Iron Plate: +100 DF. Blacksmith forges it mid-fight with 4+ allies on her Anvil mark; never in the shop.
    internal static class MasterworkIronPlate
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.DF, 100f));
        }
    }
}
