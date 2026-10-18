using System;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;

namespace MagicSchool.Combat.Placements
{
    // for battle setup: which hero? on which team? standing where? wearing what?
    [Serializable]
    public struct HeroPlacement
    {
        public HeroDataSO dataSO;
        public TeamEnum team;
        public HexNumber hexPlacement;

        // Combat does not reference Items, so this is a plain ScriptableObject; Core reads it as an ItemDataSO.
        [Tooltip("The item this hero wears (an ItemDataSO). Empty = nothing.")]
        public ScriptableObject item;

        [Tooltip("Which of its align patterns the hero is turned to. 0 = the first.")]
        public int alignPattern;

        public HeroPlacement(HeroDataSO data, TeamEnum team, HexNumber hexPlacement, ScriptableObject item = null, int alignPattern = 0)
        {
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
