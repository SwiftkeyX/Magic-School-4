
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Chalice of Dawn: +30 Starting Mana
    internal static class ChaliceOfDawn
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.StartMana, 30f));
        }
    }
}
