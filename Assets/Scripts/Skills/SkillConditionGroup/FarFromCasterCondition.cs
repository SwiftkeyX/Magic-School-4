using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// "Is the recipient standing at least this many hexes from the caster?" - asked when the effect lands,
    /// so it is where both stand on the hit, not where they stood on the cast.
    /// E.g. Skeleton Archer's shot is stronger against a target 3 or more hexes away
    /// </summary>
    internal class FarFromCasterCondition : SkillCondition
    {
        private readonly int _minHexes;

        public FarFromCasterCondition(int minHexes) : base(ConditionSubjectEnum.Recipient)
        {
            _minHexes = minHexes;
        }

        protected override bool IsMet(IEffectable recipient)
        {
            if (!(Subjected(recipient) is IPlaceable target)) return false;

            return _caster is ISkillCaster caster && caster.IsAtLeastHexesAway(target, _minHexes);
        }
    }
}
