
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Warden's Vow: +15 DF, +6% Damage Reduction
    internal static class WardensVow
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.DF, 15f),
                ItemFactory.Buff(ModifierEnum.DamageReduction, 6f));
        }
    }
}
