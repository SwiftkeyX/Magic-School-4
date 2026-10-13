using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;
using MagicSchool.Modifiers;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Align
{
    // FLAGGING: I don't like that the modifier, and the hex was serializable and was edited in the inspector. I prefer it inside code.
    // But it was fine for now. I need to make it fast first.

    /// <summary>
    /// AlignMark = a list of hexes that give the same bonus. 
    /// p.s. a hero with two different bonuses has two AlignMarks.
    /// 
    /// Three main things it does:
    ///     1. Keep a list hexes that was marked
    ///     2. Keep a modifier that was granted to the ally standing on a marked hex
    ///     3. Keep a modifier that was granted to the owner itself, once for every marked hex an ally stands on
    /// e.g.    Ettin: [SW 1, NW 1], AS, 30, percent of AS      => a hero on either hex behind him gains 30% of its own attack speed
    /// e.g.    Centaur: [SW 3], BonusHP, 200, None             => the hero behind-below him for 3 hex, gains 200 HP
    /// e.g.    [NE 2], ..., self ATK, 10, percent of ATK       => the owner gains 10% of its own AD, when an ally stands 2 hexes ahead-above
    /// </summary>
    [Serializable]
    public struct AlignMark
    {
        [Tooltip("Every hex that gives this bonus.")]
        public List<AlignHex> hexes;

        [Header("Ally bonus")]
        [Tooltip("What the ally standing there gets.")]
        public ModifierEnum modifier;

        [Tooltip("0 = the ally gets nothing.")]
        public float amount;

        [Tooltip("None = the amount is added as it is. Otherwise the amount is a percent of this stat, read from the ally's own base stat.")]
        public StatEnum percentOf;

        [Header("Self bonus")]
        [Tooltip("What this hero itself gets.")]
        public ModifierEnum selfModifier;

        [Tooltip("0 = this mark gives its owner nothing.")]
        public float selfAmount;

        [Tooltip("None = the amount is added as it is. Otherwise the amount is a percent of this stat, read from this hero's own base stat.")]
        public StatEnum selfPercentOf;

        private const float RestOfFight = -1f;

        // =================================== getter ===================================
        public bool HasAllyBonus => amount != 0f;
        public bool HasSelfBonus => selfAmount != 0f;

        // Get the hexes that was marked by this hero.
        public List<Hex> GetMarkedHexes(Hex standingOn)
        {
            var marked = new List<Hex>();
            if (hexes == null) return marked;

            foreach (AlignHex alignHex in hexes)
            {
                Hex hex = alignHex.GetHex(standingOn);
                if (hex != null) marked.Add(hex);
            }

            return marked;
        }

        // Get bonus modifier for ally
        public ICustomModifier BuildAllyModifier() => Build(modifier, amount, percentOf);

        // Get bonus modifier for self
        public ICustomModifier BuildSelfModifier() => Build(selfModifier, selfAmount, selfPercentOf);


        private static ICustomModifier Build(ModifierEnum modifier, float amount, StatEnum percentOf)
        {
            StatRatio ratio = percentOf == StatEnum.None
                ? new StatRatio(amount)
                : new StatRatio(percentOf, amount, ScaleFromEnum.Base);

            IModifier bonus = new StatModifier(modifier, new[] { ratio }, ScalingSourceEnum.Recipient);

            return new CustomModifier(RestOfFight, new[] { bonus }, isStack: true);
        }
    }

    /// <summary>
    /// A hex that was marked by the aligning mechanic, 
    /// keep a direction from the hero, and how many hexes away.
    /// e.g.    SW, 1   => the hex behind-below the hero
    /// e.g.    NE, 2   => 2 hexes ahead-above the hero
    /// </summary>
    [Serializable]
    public struct AlignHex
    {
        [Tooltip("The direction of the marked hex from this hero.")]
        public HexDirectionEnum direction;

        [Tooltip("How many hexes away in that direction.")]
        public int distance;

        public int Distance => Mathf.Max(1, distance);

        public Hex GetHex(Hex standingOn) => standingOn.GetNeighbor(direction, Distance);
    }
}
