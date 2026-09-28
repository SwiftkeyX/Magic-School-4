
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Oaken Charm: +250 HP
    internal static class OakenCharm
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.BonusHP, 250f));
        }
    }
}
