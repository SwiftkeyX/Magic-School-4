
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Hunter's Cord: +0.30 Attack Speed
    internal static class HuntersCord
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.AS, 0.3f));
        }
    }
}
