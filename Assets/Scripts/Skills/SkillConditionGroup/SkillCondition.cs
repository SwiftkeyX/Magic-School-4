using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// A question a skill asks before it does something. One predicate, used in two places that do
    /// different things with the answer:
    ///
    /// 1) on a SkillActionGroup, it gates - a group whose condition fails is skipped, and the step
    ///    falls through to the next group. That is how a template action gets swapped for another
    ///    one, e.g. Vharn throwing a box while transformed and a circle otherwise.
    ///
    /// 2) on a SkillEffect, it amplifies - the effect still lands, but scaled up when the answer is
    ///    yes, e.g. +30% damage against a wounded target.
    ///
    /// The consequence differs, the question does not, so it is written once here.
    /// </summary>
    public abstract class SkillCondition
    {
        protected ConditionSubjectEnum _subject;
        protected IEffectable _caster;

        protected SkillCondition(ConditionSubjectEnum subject)
        {
            _subject = subject;
        }

        public void Init(IEffectable caster) => _caster = caster;

        // the actual condition - read each child for more detail
        protected abstract bool IsMet(IEffectable recipient);

        // condition look at which hero?
        protected IEffectable Subjected(IEffectable recipient)
        {
            if (_subject == ConditionSubjectEnum.Caster)
            {
                if (_caster == null)
                {
                    Debug.LogError($"[{GetType().Name}] asks about the caster but was never given one. " +
                                   "SkillDefinition.Init() has to reach every condition it holds.");
                }

                return _caster;
            }

            else if (_subject == ConditionSubjectEnum.Recipient)
            {
                if (recipient == null)
                {
                    Debug.LogError($"[{GetType().Name}] asks about the recipient but was asked without one. " +
                                   "Only a SkillEffect's conditions get a recipient, not a SkillActionGroup's.");
                }

                return recipient;
            }

            else
            {
                Debug.LogError($"[{GetType().Name}] has no idea who {_subject} is meant to be.");
                return null;
            }
        }

        // to tell the caller "Is all the condition met?"
        public static ConditionResultEnum Ask(List<SkillCondition> conditions, IEffectable recipient = null)
        {
            if (conditions == null) return ConditionResultEnum.NoConditionFound;

            bool askedAnything = false;

            foreach (SkillCondition condition in conditions)
            {
                // guard
                if (condition == null) { Debug.LogError("[SkillCondition] a null condition sits in this list - skipped."); continue; }

                askedAnything = true;

                // if either condition isn't satisfied, return conditionNotMet
                if (!condition.IsMet(recipient)) return ConditionResultEnum.ConditionIsNotMet;
            }

            return askedAnything
                ? ConditionResultEnum.ConditionIsMet
                : ConditionResultEnum.NoConditionFound;
        }
    }
}
