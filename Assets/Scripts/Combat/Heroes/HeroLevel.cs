using UnityEngine;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// An enemy's level: how a stage makes the same hero stronger. Set per enemy in the stage (HeroPlacement.level).
    /// Linear: every level past 1 adds 10% of the base HP, AD, AP and DF; attack speed, mana and range stay.
    /// e.g.    level 1  => 100%
    ///         level 11 => 200%
    ///         level 30 => 390%
    /// It stacks with stars (multiplied), though a stage enemy is always 1 star.
    /// </summary>
    public static class HeroLevel
    {
        public const int Min = 1;
        public const int Max = 30;

        private const float PerLevel = 0.1f;

        // a level outside 1-30 is read as the nearest one; 0 is an entry saved before levels existed, so level 1
        public static int Clamp(int level) => Mathf.Clamp(level, Min, Max);

        public static float Multiplier(int level) => 1f + PerLevel * (Clamp(level) - Min);
    }
}
