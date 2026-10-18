using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    public abstract class SkillEffect
    {
        protected EffectRecipientEnum _recipient;        // the list of recipients who get effect
        protected Cadence _cadence = new Cadence();      // Is effect reapply over time?
        protected List<SkillCondition> _conditions;      // conditions for this effect. => if all satisfied, effect is amplified.
        protected float _amplifier = 0.3f;               // e.g. 0.3 = +30% when the conditions hold
        protected Func<IEffectable, float> _amplifierOf; // when set, the amplifier is worked out from the caster at the hit instead
        protected IEffectable _caster;                   // whose stats the amounts scale off

        protected SkillEffect(EffectRecipientEnum recipient, Cadence cadence = null,
                              List<SkillCondition> conditions = null, float amplifier = 0.3f)
        {
            _recipient = recipient;
            _cadence = cadence ?? new Cadence();
            _conditions = conditions ?? new List<SkillCondition>();
            _amplifier = amplifier;
        }

        // ================================== getter ==================================
        public EffectRecipientEnum Recipient => _recipient;
        public Cadence Cadence => _cadence;
        public List<SkillCondition> Conditions => _conditions;

        // the conditions for this effect, it need to know who cast it.
        public void Init(IEffectable caster)
        {
            _caster = caster;

            foreach (SkillCondition condition in _conditions) condition?.Init(caster);
        }

        // check condition to each recipients.
        // If condition is met, the effect is amplified.
        protected float AmplifierFor(IEffectable recipient)
        {
            // an amplifier worked out from the caster, with no condition to ask, applies to every hit
            // e.g. Dwarf's blast deals more the bigger it was fired
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
