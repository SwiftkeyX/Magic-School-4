using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    // for battle setup: which hero? on which team? standing where? wearing what?
    // item is a plain ScriptableObject because Combat does not reference Items; Core reads it as an ItemDataSO. A
    // level or star of 0 or out of range reads as the nearest valid one (0 = saved before it existed).
    [Serializable]
    public struct HeroPlacement
    {
        public HeroDataSO dataSO;
        public TeamEnum team;
        public HexNumber hexPlacement;

        [Tooltip("The item this hero wears (an ItemDataSO). Empty = nothing.")]
        public ScriptableObject item;

        [Tooltip("Which of its align patterns the hero is turned to. 0 = the first.")]
        public int alignPattern;

        [Tooltip("An enemy's level, 1-30: every level past 1 adds 10% to its HP, AD, AP and DF (See HeroLevel). " +
                 "0 (an entry from before levels) is level 1.")]
        public int level;

        [Tooltip("The hero's star, 1-3 (See HeroStar). 0 (an entry from before stars were saved) is 1 star.")]
        public int star;

        public int Level => HeroLevel.Clamp(level);

        public int Star => Mathf.Clamp(star, HeroStar.Min, HeroStar.Max);

        public HeroPlacement(HeroDataSO data, TeamEnum team, HexNumber hexPlacement, ScriptableObject item = null, int alignPattern = 0,
                             int level = HeroLevel.Min, int star = HeroStar.Min)
        {
            this.level = level;
            this.star = star;
            this.dataSO = data;
            this.team = team;
            this.hexPlacement = hexPlacement;
            this.item = item;
            this.alignPattern = alignPattern;
        }
    }

    // for battle setup: an item lying on a hex, for whoever's wearing hex that is (See HexItems)
    [Serializable]
    public struct ItemPlacement
    {
        [Tooltip("The item (an ItemDataSO).")]
        public ScriptableObject item;
        public HexNumber hexPlacement;

        public ItemPlacement(ScriptableObject item, HexNumber hexPlacement)
        {
            this.item = item;
            this.hexPlacement = hexPlacement;
        }
    }
}
