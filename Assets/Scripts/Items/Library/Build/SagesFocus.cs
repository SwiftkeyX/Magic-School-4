
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Sage's Focus: +25 AP, +20 DF
    internal static class SagesFocus
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.AP, 25f),
                ItemFactory.Buff(ModifierEnum.DF, 20f));
        }
    }
}
