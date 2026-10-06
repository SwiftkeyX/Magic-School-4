
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    // Ember Brand: auto attacks set the target burning for 10% AP every second
    internal static class EmberBrand
    {
        internal static ICustomModifier Build()
        {
            return ItemFactory.Bundle(
                ItemFactory.Buff(ModifierEnum.BurnOnAttack, (StatEnum.AP, 10f)));
        }
    }
}
