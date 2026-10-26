using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    // When every condition holds for a recipient the effect is amplified (0.3 = +30%); an amplifier worked out from
    // the caster with no conditions applies to every hit (Dwarf's bigger blasts).
    public abstract class SkillEffect
    {
        protected EffectRecipientEnum _recipient;
        protected Cadence _cadence = new Cadence();
        protected List<SkillCondition> _conditions;
        protected float _amplifier = 0.3f;
        protected Func<IEffectable, float> _amplifierOf;
        protected IEffectable _caster;

        protected SkillEffect(EffectRecipientEnum recipient, Cadence cadence = null,
                              List<SkillCondition> conditions = null, float amplifier = 0.3f)
        {
            _recipient = recipient;
            _cadence = cadence ?? new Cadence();
            _conditions = conditions ?? new List<SkillCondition>();
            _amplifier = amplifier;
        }

        public EffectRecipientEnum Recipient => _recipient;
        public Cadence Cadence => _cadence;
        public List<SkillCondition> Conditions => _conditions;

        public void Init(IEffectable caster)
        {
            _caster = caster;

            foreach (SkillCondition condition in _conditions) condition?.Init(caster);
        }

        protected float AmplifierFor(IEffectable recipient)
        {
            if (_amplifierOf != null && _conditions.Count == 0) return 1f + _amplifierOf(_caster);

            if (SkillCondition.Ask(_conditions, recipient) == ConditionResultEnum.ConditionIsMet)
            {
                return 1f + (_amplifierOf != null ? _amplifierOf(_caster) : _amplifier);
            }

            return 1f;
        }

        public abstract void ApplyEffect(IReadOnlyList<IEffectable> recipients, IEffectable actor = null);
    }
}
