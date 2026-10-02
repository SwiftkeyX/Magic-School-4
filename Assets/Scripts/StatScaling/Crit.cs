using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.StatScaling
{
    // Crit Give answer to this question, "did this hit crit, and how much damage is it now?".
    // Example, 
    //          every auto attack asks this question 
    //          a skill only asks when it was built to crit
    public static class Crit
    {
        public const float Multiplier = 1.5f;     // a crit deals 150% damage

        public static float Roll(float damage, IHeroStats attacker, out bool isCrit)
        {
            float chance = attacker == null ? 0f : attacker.GetStat(StatEnum.CritChance);

            isCrit = Random.value * 100f < chance;

            return isCrit ? damage * Multiplier : damage;
        }
    }
}
