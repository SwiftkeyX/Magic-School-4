using UnityEngine;

namespace MagicSchool.Combat.Heroes
{
    /// <summary>
    /// A hero's star level (TFT's upgrade): two copies of a hero at the same star combine into one, a star higher.
    /// Each star multiplies the base stats it changes; everything else (DF, attack speed, mana, range) stays.
    /// e.g.    1 star  => 100% HP, 100% AD, 100% AP      (1 copy)
    ///         2 star  => 180% HP, 150% AD, 150% AP      (2 copies)
    ///         3 star  => 324% HP, 225% AD, 225% AP      (4 copies)
    /// HP and AD are TFT's own numbers. TFT writes a skill's numbers out per star instead of scaling AP;
    /// AP stands in for that here, so a caster's skill grows the way an AD hero's does through AD.
    /// </summary>
    public static class HeroStar
    {
        public const int Min = 1;
        public const int Max = 3;
        public const int CopiesToCombine = 2;

        private const float HpPerStar = 1.8f;
        private const float AtkPerStar = 1.5f;
        private const float ApPerStar = 1.5f;

        // how much bigger the hero is drawn per star, so an upgraded hero can be told apart on the board
        private const float ScalePerStar = 0.15f;

        public static float HpMultiplier(int star) => Mathf.Pow(HpPerStar, star - Min);
        public static float AtkMultiplier(int star) => Mathf.Pow(AtkPerStar, star - Min);
        public static float ApMultiplier(int star) => Mathf.Pow(ApPerStar, star - Min);
        public static float Scale(int star) => 1f + ScalePerStar * (star - Min);

        // how many 1-star copies went into it: what it cost, and what selling it gives back
        // e.g.     3 star => 4 copies
        public static int Copies(int star) => (int)Mathf.Pow(CopiesToCombine, star - Min);
    }
}
