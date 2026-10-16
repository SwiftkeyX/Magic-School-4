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
        private readonly DamageKindEnum _kind;  
        private readonly bool _canCrit;     // skill damage does not crit by default, unless the skill was built to
        private float _lifestealPercent;

        public AttackSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> ratios, Cadence cadence = null,
                                 List<SkillCondition> conditions = null, float amplifier = 0f, bool canCrit = false,
                                 DamageKindEnum kind = DamageKindEnum.Skill)
            : base(recipient, cadence, conditions, amplifier)
        {
            _ratios = ratios;
            _canCrit = canCrit;
            _kind = kind;
        }

        // This attack hardcode the lifesteal in. Not related to Omnivamp stat.
        // e.g.     Damage(...).WithLifesteal(100f)      Priest's fairy heals its owner, not the Priest
        public AttackSkillEffect WithLifesteal(float percent)
        {
            _lifestealPercent = percent;
            return this;
        }

        // The amplifier its conditions give, worked out from the caster when the hit lands, in place of a fixed one.
        // e.g.     .AmplifiedBy(caster => 0.15f * allies on its marks)     Skeleton Archer's long shots
        public AttackSkillEffect AmplifiedBy(System.Func<IEffectable, float> amplifier)
        {
            _amplifierOf = amplifier;
            return this;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null)
            => ApplyEffect(recipients, actor, 1f);

        // [scale] multiplies the whole damage, e.g. 0.67 for the second enemy a falling-off piercing shot goes through
        public void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor, float scale)
        {
            // scale the damage e.g. skill damage = 500% AP
            float damageAmount = Scaling.Total(_ratios, _caster as IHeroStats) * scale;

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
                    kind: _kind
                );

                // if caster have omnivamp, heals off the damage dealt
                Omnivamp.Apply(_caster, landed);

                // if this effect have lifesteal, whoever played the action heals off the damage dealt
                Omnivamp.HealFromDamage(actor, landed, _lifestealPercent, source: _caster);
            }
        }
    }
}
