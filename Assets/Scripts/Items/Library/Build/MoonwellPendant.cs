using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Moonwell Pendant: +2 Mana per second
    internal static class MoonwellPendant
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.ManaPerSecond, 2f));
        }
    }
}
