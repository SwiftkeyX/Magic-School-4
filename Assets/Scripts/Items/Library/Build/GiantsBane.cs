using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Giant's Bane: +30% of base Atk, +40% Armor Penetration - like LoL's Lord Dominik's Regards: a percent, so it matters most against high DF
    internal static class GiantsBane
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 30f, ScaleFromEnum.Base)),
                ItemFactory.Buff(ModifierEnum.ArmorPen, 40f));
        }
    }
}
