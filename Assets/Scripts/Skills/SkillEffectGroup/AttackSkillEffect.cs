using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Skills
{
    // effect that apply damage to recipients
    internal class AttackSkillEffect : SkillEffect
    {
        private readonly IReadOnlyList<StatRatio> _ratios;
        private readonly bool _canCrit;     // skill damage does not crit by default, unless the skill was built to

        public AttackSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> ratios, Cadence cadence = null,
                                 List<SkillCondition> conditions = null, float amplifier = 0f, bool canCrit = false)
            : base(recipient, cadence, conditions, amplifier)
        {
            _ratios = ratios;
            _canCrit = canCrit;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients)
        {
            // scale the damage e.g. skill damage = 500% AP
            float damageAmount = Scaling.Total(_ratios, _caster as IHeroStats);

            foreach (IEffectable recipient in recipients)
            {
                if (recipient == null || !recipient.IsAlive) continue;

                // if the damage was cadence, calculate tick damage instead
                float dmg;
                if (_cadence.isCadence) dmg = damageAmount * _cadence.cadenceInterval / _cadence.cadenceDuration;

                // if not cadence, apply the whole amount at once
                else dmg = damageAmount;

                // if pass specify condition, amplify the effect
                dmg *= AmplifierFor(recipient);

                // roll the crit once per recipient
                if (_canCrit) dmg = Crit.Roll(dmg, _caster as IHeroStats, out _);

                // apply damage
                recipient.TakeDamage(
                    damage: Mathf.RoundToInt(dmg),
                    source: _caster,
                    kind: DamageKindEnum.Skill
                );
            }
        }
    }
}
