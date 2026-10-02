using MagicSchool.Contracts;

namespace MagicSchool.StatScaling
{
    // Omnivamp Give answer to this question, "how much does the attacker heal off the damage he just dealt?".
    // Example,
    //          every auto attack asks this question
    //          every skill damage asks this question
    //
    // There is 2 type of omnivamp:
    //          1) omnivamp stat: the attacker always heal himself after damage dealt, if he have the Omnivamp stat. 
    //          2) lifesteal skill: the skill that hardcode the lifesteal in. not relate ot Omnivamp stat.
    public static class Omnivamp
    {
        // the attacker heal from the damage he dealt.
        // e.g.     40% omnivamp, do 100 dmg => heals 40
        public static void Apply(IEffectable attacker, int landed)
        {
            float percent = attacker is IHeroStats stats ? stats.GetStat(StatEnum.Omnivamp) : 0f;

            HealFromDamage(attacker, landed, percent, source: attacker);
        }

        // heal someone by dealt damage.
        // different to Apply(), this one can heal ally, not just the attacker himself.
        // e.g.     100% heal, do 50 dmg => heals 50
        public static void HealFromDamage(IEffectable healed, int landed, float percent, IEffectable source)
        {
            if (healed == null || landed <= 0 || percent <= 0f) return;

            // can't heal dead hero.
            if (!healed.IsAlive) return;

            healed.Heal(landed * percent / 100f, source);
        }
    }
}
