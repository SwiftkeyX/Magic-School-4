
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Runed Edge: +8 Atk, +25 AP
    internal static class RunedEdge
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.ATK, 8f),
                ItemFactory.Buff(ModifierEnum.AP, 25f));
        }
    }
}
