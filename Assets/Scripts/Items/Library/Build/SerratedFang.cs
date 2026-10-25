using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Serrated Fang: +30% of base Atk, +15 Lethality - like LoL's Serrated Dirk: flat, so it matters most against low DF
    internal static class SerratedFang
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 30f, ScaleFromEnum.Base)),
                ItemFactory.Buff(ModifierEnum.Lethality, 15f));
        }
    }
}
