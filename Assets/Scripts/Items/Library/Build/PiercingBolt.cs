using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Items
{
    // Piercing Bolt: +30% of base Atk, +25% Armor Penetration - like LoL's Last Whisper
    internal static class PiercingBolt
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, new StatRatio(StatEnum.ATK, 30f, ScaleFromEnum.Base)),
                ItemFactory.Buff(ModifierEnum.ArmorPen, 25f));
        }
    }
}
