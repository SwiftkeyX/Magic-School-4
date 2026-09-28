using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    // effect that apply modifer to a recipients
    // e.g. apply wound, apply buff, apply stun, etc...
    internal class ModifierSkillEffect : SkillEffect
    {
        private readonly ICustomModifier _modifier;

        public ICustomModifier Modifier => _modifier;
 
        public ModifierSkillEffect(EffectRecipientEnum recipient, ICustomModifier modifier, Cadence cadence = null,
                                   List<SkillCondition> conditions = null, float amplifier = 0f)
            : base(recipient, cadence, conditions, amplifier)
        {
            _modifier = modifier;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients)
        {
            if (_modifier == null) return;

            foreach (IEffectable recipient in recipients)
            {
                if (recipient == null || !recipient.IsAlive) continue;

                recipient.AddModifier(_modifier, AmplifierFor(recipient), _caster as IHeroStats);
            }
        }
    }
}
