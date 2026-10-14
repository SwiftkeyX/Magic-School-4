
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Reaver's Edge: +30% of base Atk, heals for 15% of damage dealt
    internal static class ReaversEdge
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 30f, ScaleFromEnum.Base)),
                ItemFactory.Buff(ModifierEnum.Omnivamp, 15f));
        }
    }
}
