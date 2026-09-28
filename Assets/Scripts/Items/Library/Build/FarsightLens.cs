
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Farsight Lens: +1 Range, +20 AP
    internal static class FarsightLens
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.Range, 1f),
                ItemFactory.Buff(ModifierEnum.AP, 20f));
        }
    }
}
