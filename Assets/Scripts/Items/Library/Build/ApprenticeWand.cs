
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Apprentice Wand: +30 AP
    internal static class ApprenticeWand
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.AP, 30f));
        }
    }
}
