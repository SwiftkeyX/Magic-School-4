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
        private float _lifestealPercent;

        public AttackSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> ratios, Cadence cadence = null,
                                 List<SkillCondition> conditions = null, float amplifier = 0f, bool canCrit = false)
            : base(recipient, cadence, conditions, amplifier)
        {
            _ratios = ratios;
            _canCrit = canCrit;
        }

        // This attack hardcode the lifesteal in. Not related to Omnivamp stat.
        // e.g.     Damage(...).WithLifesteal(100f)      Priest's fairy heals its owner, not the Priest
        public AttackSkillEffect WithLifesteal(float percent)
        {
            _lifestealPercent = percent;
            return this;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null)
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
                int landed = recipient.TakeDamage(
                    damage: Mathf.RoundToInt(dmg),
                    source: _caster,
                    kind: DamageKindEnum.Skill
                );

                // if caster have omnivamp, heals off the damage dealt
                Omnivamp.Apply(_caster, landed);

                // if this effect have lifesteal, whoever played the action heals off the damage dealt
                Omnivamp.HealFromDamage(actor, landed, _lifestealPercent, source: _caster);
            }
        }
    }
}
