using MagicSchool.Contracts;

namespace MagicSchool.Modifiers
{
    public class StatusModifier : IModifier
    {
        private readonly ModifierEnum _status;

        public StatusModifier(ModifierEnum status)
        {
            _status = status;
        }

        // === IModifier ===
        public ModifierEnum GetModifierEnum() => _status;

        // FLAGGING: When we see pattern more clear, we maybe consider separate
        // StatusModifer & StatModifier to not use the same interface.
        // Status don't need scaling source
        public ScalingSourceEnum GetScalingSource() => ScalingSourceEnum.Caster;
        
        // FLAGGING: The status actually can carry amount. BUT it isn't a input one in skill builder.
        // e.g. Wound have fix amount = 50% heal reduction.
        // e.g. Burn do 3% fix amount dmg to enemy.
        // e.g. Stun don't have amount. 
        public float GetBonusAmount(IHeroStats stats) => 0f;
    }
}
