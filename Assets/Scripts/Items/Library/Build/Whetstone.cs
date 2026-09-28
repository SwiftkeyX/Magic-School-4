
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Whetstone: +15 Atk
    internal static class Whetstone
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, 15f));
        }
    }
}
