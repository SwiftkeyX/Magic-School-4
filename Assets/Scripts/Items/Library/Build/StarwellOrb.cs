using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Starwell Orb: +3 Mana per second
    internal static class StarwellOrb
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.ManaPerSecond, 3f));
        }
    }
}
