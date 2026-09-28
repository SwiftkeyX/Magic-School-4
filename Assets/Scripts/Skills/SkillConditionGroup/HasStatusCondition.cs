using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// "Is the subject carrying this status right now?"
    /// E.g. transformed, wounded, stunned, ...
    /// </summary>
    internal class HasStatusCondition : SkillCondition
    {
        private ModifierEnum _status;
        private bool _wantPresent = true;

        public HasStatusCondition(ConditionSubjectEnum subject, ModifierEnum status, bool wantPresent = true)
            : base(subject)
        {
            _status = status;
            _wantPresent = wantPresent;
        }

        protected override bool IsMet(IEffectable recipient)
        {
            // ask which unit?
            IEffectable subject = Subjected(recipient);

            // guard
            if (subject == null) return false;

            // this unit has/hasn't this staus
            return subject.HasStatus(_status) == _wantPresent;
        }
    }
}
