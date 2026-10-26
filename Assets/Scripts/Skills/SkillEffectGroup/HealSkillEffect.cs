using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Skills
{
    // FLAGGING: Try combine into ModifierSkillEffect. Not working, since the ModifierSkillEffect already work well.
    // It still look weird to me but it work well and clean. Lets leave it.
    internal class HealSkillEffect : SkillEffect
    {
        private readonly IReadOnlyList<StatRatio> _ratios;
        private readonly float _duration;

        public float Duration => _duration;

        public HealSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> ratios, float duration = -1f,
                               Cadence cadence = null, List<SkillCondition> conditions = null, float amplifier = 0.3f)
            : base(recipient, cadence, conditions, amplifier)
        {
            _ratios = ratios;
            _duration = duration;
        }

        public HealSkillEffect AmplifiedBy(System.Func<IEffectable, float> amplifier)
        {
            _amplifierOf = amplifier;
            return this;
        }

        public HealSkillEffect Scaled(System.Func<IEffectable, float> multiplier)
        {
            _scaleOf = multiplier;
            return this;
        }

        // the heal over time lasts as long as this says when it is played, rather than the duration it was built with;
        // the ticks are still counted from that one, so scale them if the whole heal must stay the same
        public HealSkillEffect LastingFor(System.Func<float> seconds)
        {
            Cadence.LastingFor(seconds);
            return this;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null)
        {
            int totalTicks = Mathf.Max(1, Mathf.RoundToInt(_duration / Cadence.cadenceInterval));
            float healPerTick = Scaling.Total(_ratios, _caster as IHeroStats) / totalTicks;

            foreach (IEffectable recipient in recipients)
            {
                if (recipient == null || !recipient.IsAlive) continue;

                recipient.Heal(
                    amount: healPerTick * AmplifierFor(recipient),
                    source: _caster
                );
            }
        }
    }
}
