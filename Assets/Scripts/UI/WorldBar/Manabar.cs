namespace MagicSchool.UI
{
    // World-space mana bar - fills as the hero attacks, and empties when the skill is cast.
    internal class Manabar : WorldBar
    {
        protected override float Fill => (float)_hero.CurrentMana / _hero.MaxMana;
    }
}
