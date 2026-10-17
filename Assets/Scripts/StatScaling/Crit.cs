using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.StatScaling
{
    // Crit Give answer to this question, "did this hit crit, and how much damage is it now?".
    // Example,
    //          every auto attack asks this question
    //          a skill only asks when it was built to crit
    //
    // The roll is pseudo-random (the PRD of Dota / Warcraft 3), not a plain coin flip. Each attacker keeps
    // a count of hits since its last crit, and the n-th hit crits at C * n - so a crit grows likelier the
    // longer it has not come, and resets once it does. C is worked out from the crit chance so that, over
    // many hits, the crit rate is still exactly the crit chance: average damage is unchanged, only the
    // streaks are gone.
    // e.g.     25% crit chance => C = 8.5%: 8.5%, 17%, 25.5%, 34%... until it crits
    public static class Crit
    {
        public const float Multiplier = 1.5f;     // a crit deals 150% damage

        // hits since each attacker's last crit, auto attacks and crit-able skills together.
        // Keyed weakly, so a destroyed hero takes its count with it.
        private class Streak { public int hits; }
        private static readonly ConditionalWeakTable<IHeroStats, Streak> _streaks = new ConditionalWeakTable<IHeroStats, Streak>();

        // C for each crit chance, keyed by the chance in hundredths of a percent
        private static readonly Dictionary<int, float> _constants = new Dictionary<int, float>();

        public static float Roll(float damage, IHeroStats attacker, out bool isCrit)
        {
            float chance = attacker == null ? 0f : attacker.GetStat(StatEnum.CritChance);

            isCrit = RollPseudoRandom(attacker, chance / 100f);

            return isCrit ? damage * Multiplier : damage;
        }

        // start the attacker's count again, so a fight does not open on a crit owed from the last one
        public static void ResetStreak(IHeroStats attacker)
        {
            if (attacker != null) _streaks.Remove(attacker);
        }

        private static bool RollPseudoRandom(IHeroStats attacker, float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;

            // chance can change mid-fight (aligning, items): C follows it, the count is kept
            Streak streak = _streaks.GetOrCreateValue(attacker);
            streak.hits++;

            bool isCrit = Random.value < ConstantFor(chance) * streak.hits;
            if (isCrit) streak.hits = 0;

            return isCrit;
        }

        private static float ConstantFor(float chance)
        {
            int key = Mathf.RoundToInt(chance * 10000f);
            if (_constants.TryGetValue(key, out float c)) return c;

            c = SolveConstant(key / 10000f);
            _constants[key] = c;
            return c;
        }

        // C has no closed form: binary search for the C whose average crit rate is [chance]
        private static float SolveConstant(float chance)
        {
            double low = 0d, high = chance;
            for (int i = 0; i < 50; i++)
            {
                double mid = (low + high) / 2d;
                if (AverageRate(mid) < chance) low = mid;
                else high = mid;
            }
            return (float)((low + high) / 2d);
        }

        // the crit rate C gives over many hits: 1 / the average number of hits it takes to crit
        private static double AverageRate(double c)
        {
            double critBefore = 0d, averageHits = 0d;
            int maxHits = (int)System.Math.Ceiling(1d / c);

            for (int n = 1; n <= maxHits; n++)
            {
                double critOnN = System.Math.Min(1d, c * n) * (1d - critBefore);
                critBefore += critOnN;
                averageHits += n * critOnN;
            }
            return 1d / averageHits;
        }
    }
}
