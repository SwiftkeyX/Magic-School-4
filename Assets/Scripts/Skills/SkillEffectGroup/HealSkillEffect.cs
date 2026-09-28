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

        // heal the reciepient
        // if the heal was cadence, it only heal the divided amount each time.
        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients)
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
