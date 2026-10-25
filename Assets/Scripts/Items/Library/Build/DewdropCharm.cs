using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Dewdrop Charm: +1 Mana per second
    internal static class DewdropCharm
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(ItemFactory.Buff(ModifierEnum.ManaPerSecond, 1f));
        }
    }
}
