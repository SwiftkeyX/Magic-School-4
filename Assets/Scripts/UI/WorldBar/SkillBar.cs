namespace MagicSchool.UI
{
    /// <summary>
    /// worldbar that counting down the duration of CustomModifier.
    /// e.g. the ten seconds Werewolf spends transformed.
    /// A hero can carry several SkillBar at once.
    /// </summary>
    internal class SkillBar : WorldBar
    {
        public const int None = -1;     // bound to no modifier: the bar stays hidden

        private int _index = None;

        // which of the hero's modifiers this bar counts down (See SkillBarStack)
        public void Bind(int index) => _index = index;

        protected override float Fill => _hero.ModifierRemaining(_index);

        protected override bool IsShown => _index != None && _index < _hero.ActiveModifierCount && Fill > 0f;
    }
}
