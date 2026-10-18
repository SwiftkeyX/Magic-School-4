
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Whetstone: +50% of base Atk
    // A percent, not a flat number: base AD is ~55 where base AP is 100, so a flat +15 Atk was only +27%
    // next to Archmage Tome's +55%. A percent also keeps it an AD-hero item - a 30 AD caster gets little.
    internal static class Whetstone
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 50f, ScaleFromEnum.Base)));
        }
    }
}
