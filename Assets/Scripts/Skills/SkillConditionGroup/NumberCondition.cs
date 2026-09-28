using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    internal class NumberCondition : SkillCondition
    {
        // shared with the other beats of this combo, on purpose
        private ComboTracker _combo;
        private int _matchBeat;

        public NumberCondition(ConditionSubjectEnum subject, ComboTracker combo, int matchBeat) : base(subject)
        {
            _combo = combo;
            _matchBeat = matchBeat;
        }

        protected override bool IsMet(IEffectable recipient)
        {
            if (_combo == null)
            {
                Debug.LogError($"[{nameof(NumberCondition)}] was asked without a combo to read. A skill " +
                               "authored in a SkillSO cannot supply one - it has to be built by SkillLibrary.");
                return false;
            }

            return _combo.Beat == _matchBeat;
        }
    }
}
