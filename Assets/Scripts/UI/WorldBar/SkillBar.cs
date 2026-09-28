namespace MagicSchool.UI
{
    /// <summary>
    /// worldbar that counting down the duration of CustomModifier.
    /// e.g. the ten seconds Vharn spends transformed. 
    /// A hero can carry several SkillBar at once.
    /// </summary>
    internal class SkillBar : WorldBar
    {
        private int _index;

        public void Bind(int index) => _index = index;

        protected override float Fill => _hero.ModifierRemaining(_index);

        protected override bool IsShown => _index < _hero.ActiveModifierCount && Fill > 0f;
    }
}
