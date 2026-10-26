using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;

namespace MagicSchool.Skills
{
    // effect that apply damage to recipients
    // WithLifesteal heals whoever played the action, apart from Omnivamp (Priest's fairy heals its owner). WhenLanded
    // is told what each hit really dealt, after armour and shields (Knight's shield). FullOnlyWhen: a recipient the
    // condition fails for takes [otherwise] of the damage (Skeleton Archer's close shots, half). AmplifiedBy(caster =>
    // x) works the amplifier out at the hit; with no conditions it applies to every hit. An amplifier adds to 1x: an
    // amplifier of n - 1 is x n. The crit is rolled once per recipient.
    internal class AttackSkillEffect : SkillEffect
    {
        private readonly IReadOnlyList<StatRatio> _ratios;
        private readonly DamageKindEnum _kind;  
        private readonly bool _canCrit;
        private float _lifestealPercent;
        private System.Action<int> _onLanded;
        private SkillCondition _fullWhen;
        private float _otherwise = 1f;

        public AttackSkillEffect(EffectRecipientEnum recipient, IReadOnlyList<StatRatio> ratios, Cadence cadence = null,
                                 List<SkillCondition> conditions = null, float amplifier = 0f, bool canCrit = false,
                                 DamageKindEnum kind = DamageKindEnum.Skill)
            : base(recipient, cadence, conditions, amplifier)
        {
            _ratios = ratios;
            _canCrit = canCrit;
            _kind = kind;
        }

        public AttackSkillEffect WithLifesteal(float percent)
        {
            _lifestealPercent = percent;
            return this;
        }

        public AttackSkillEffect WhenLanded(System.Action<int> onLanded)
        {
            _onLanded = onLanded;
            return this;
        }

        public AttackSkillEffect FullOnlyWhen(SkillCondition condition, float otherwise)
        {
            _fullWhen = condition;
            _otherwise = otherwise;
            return this;
        }

        public AttackSkillEffect AmplifiedBy(System.Func<IEffectable, float> amplifier)
        {
            _amplifierOf = amplifier;
            return this;
        }

        public override void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null)
            => ApplyEffect(recipients, actor, 1f);

        public void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor, float scale)
        {
            float damageAmount = Scaling.Total(_ratios, _caster as IHeroStats) * scale;

            foreach (IEffectable recipient in recipients)
            {
                if (recipient == null || !recipient.IsAlive) continue;

                float dmg;
                if (_cadence.isCadence) dmg = damageAmount * _cadence.cadenceInterval / _cadence.cadenceDuration;

                else dmg = damageAmount;

                dmg *= AmplifierFor(recipient);

                if (_fullWhen != null)
                {
                    _fullWhen.Init(_caster);
                    if (SkillCondition.Ask(new List<SkillCondition> { _fullWhen }, recipient) != ConditionResultEnum.ConditionIsMet)
                        dmg *= _otherwise;
                }

                if (_canCrit) dmg = Crit.Roll(dmg, _caster as IHeroStats, out _);

                int landed = recipient.TakeDamage(
                    damage: Mathf.RoundToInt(dmg),
                    source: _caster,
                    kind: _kind
                );

                Omnivamp.Apply(_caster, landed);

                Omnivamp.HealFromDamage(actor, landed, _lifestealPercent, source: _caster);

                _onLanded?.Invoke(landed);
            }
        }
    }
}
