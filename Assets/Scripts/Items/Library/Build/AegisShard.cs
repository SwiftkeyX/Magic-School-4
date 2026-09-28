
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Aegis Shard: +10% Damage Reduction
    internal static class AegisShard
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.DamageReduction, 10f));
        }
    }
}
