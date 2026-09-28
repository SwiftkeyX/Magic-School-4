
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Vitalis Weave: +200 HP, +5% Damage Reduction
    internal static class VitalisWeave
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.BonusHP, 200f),
                ItemFactory.Buff(ModifierEnum.DamageReduction, 5f));
        }
    }
}
