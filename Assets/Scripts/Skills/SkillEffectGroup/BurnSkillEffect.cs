using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Skills
{
    // effect that set the recipients burning
    // The damage is not dealt here: a burning hero ticks by himself, read Modifiers/Burn.cs.
    internal class BurnSkillEffect : SkillEffect
    {
        private readonly IReadOnlyList<StatRatio> _perSecond;    // the damage per second, not the total
        private readonly float _duration;

        // the cadence is only for a zone: how often it sets whoever stands in it burning again.
        public BurnSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> perSecond, float duration,
                               Cadence cadence = null)
            : base(recipient, cadence)
        {
            _perSecond = perSecond;
            _duration = duration;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null)
        {
            // scale the damage e.g. burn = 30% AP every second
            float damagePerSecond = Scaling.Total(_perSecond, _caster as IHeroStats);

            foreach (IEffectable recipient in recipients)
            {
                if (recipient == null || !recipient.IsAlive) continue;

                recipient.Ignite(damagePerSecond, _duration, source: _caster);
            }
        }
    }
}
