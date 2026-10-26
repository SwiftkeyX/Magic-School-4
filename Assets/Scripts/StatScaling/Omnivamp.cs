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
        public static void Apply(IEffectable attacker, int landed)
        {
            float percent = attacker is IHeroStats stats ? stats.GetStat(StatEnum.Omnivamp) : 0f;

            HealFromDamage(attacker, landed, percent, source: attacker);
        }

        public static void HealFromDamage(IEffectable healed, int landed, float percent, IEffectable source)
        {
            if (healed == null || landed <= 0 || percent <= 0f) return;

            if (!healed.IsAlive) return;

            healed.Heal(landed * percent / 100f, source);
        }
    }
}
