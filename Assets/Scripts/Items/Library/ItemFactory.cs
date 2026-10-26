using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using MagicSchool.Modifiers;

namespace MagicSchool.Items
{
    // An item's grant is permanent: it lasts as long as the item is worn. Buff(DamageReduction, 20f) is a flat +20;
    // Buff(ATK, (StatEnum.AP, 50f)) is 50% of the wearer's AP.
    internal static class ItemFactory
    {
        private const float Permanent = -1f;

        public static ICustomModifier Bundle(params IModifier[] modifiers)
            => new CustomModifier(Permanent, modifiers, isStack: false);

        public static IModifier Buff(ModifierEnum modifier, params StatRatio[] ratios)
            => new StatModifier(modifier, ratios);
    }
}
