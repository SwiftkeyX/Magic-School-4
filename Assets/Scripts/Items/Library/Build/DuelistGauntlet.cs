
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Duelist's Gauntlet: +30% of base Atk, +0.15 Attack Speed
    internal static class DuelistGauntlet
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 30f, ScaleFromEnum.Base)),
                ItemFactory.Buff(ModifierEnum.AS, 0.15f));
        }
    }
}
