
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Longshot Quiver: +1 Range, +0.12 Attack Speed
    internal static class LongshotQuiver
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.Range, 1f),
                ItemFactory.Buff(ModifierEnum.AS, 0.12f));
        }
    }
}
