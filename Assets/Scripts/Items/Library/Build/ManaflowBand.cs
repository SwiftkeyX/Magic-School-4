
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Manaflow Band: +5 mana per auto attack
    internal static class ManaflowBand
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ManaPerAttack, 5f));
        }
    }
}
