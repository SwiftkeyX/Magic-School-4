using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Modifiers
{
    // The actual modifier class
    // To contain modifier type and its amount in 1 place.
    public class StatModifier : IModifier
    {
        private readonly ModifierEnum _modifier;
        private readonly IReadOnlyList<StatRatio> _ratios;
        private readonly ScalingSourceEnum _source;

        public StatModifier(ModifierEnum modifier, IReadOnlyList<StatRatio> ratios,
                            ScalingSourceEnum source = ScalingSourceEnum.Caster)
        {
            _modifier = modifier;
            _ratios = ratios;
            _source = source;
        }

        // === IModifier ===
        public ModifierEnum GetModifierEnum() => _modifier;
        public ScalingSourceEnum GetScalingSource() => _source;
        public float GetBonusAmount(IHeroStats stats) => Scaling.Total(_ratios, stats);
    }
}
