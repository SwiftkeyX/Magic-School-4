using System;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;
using MagicSchool.Modifiers;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// AlignPattern = one way a hero can be turned: every mark it has while turned this way.
    /// A hero has one pattern or more, each authored by hand in the Align Editor - a pattern does not have to be
    /// the one before it turned, it can mark other hexes. Rotating a hero switches it to its next pattern.
    /// The bonuses are not the pattern's: orange and green pay the same in every pattern of a hero. Each pattern still
    /// stores its own copy, and the Align Editor keeps the copies equal (pattern 1's win).
    /// e.g.    Elf: 6 patterns, the same fan turned 60 degrees each time
    /// e.g.    a hero with 1 pattern cannot rotate
    /// </summary>
    [Serializable]
    public class AlignPattern
    {
        [Tooltip("Every mark this hero has while turned this way.")]
        public List<AlignMark> marks = new List<AlignMark>();
    }

    /// <summary>
    /// AlignMark = a list of hexes that give the same bonus.
    /// p.s. a hero with two different bonuses has two AlignMarks.
    ///
    /// Three main things it does:
    ///     1. Keep a list hexes that was marked
    ///     2. Keep a modifier that was granted to the ally standing on a marked hex
    ///     3. Keep a modifier that was granted to the owner itself, once for every marked hex an ally stands on
    /// e.g.    Ettin: [SW 1, NW 1], AS, 30, percent of AS      => a hero on either hex behind him gains 30% of its own attack speed
    /// e.g.    Ettin: ..., AS, 25, percent of AS, per owner AP  => the same, but 25% for every 100 AP Ettin has
    /// e.g.    Centaur: [SW 3], BonusHP, 200, None             => the hero behind-below him for 3 hex, gains 200 HP
    /// e.g.    [NE 2], ..., self ATK, 10, percent of ATK       => the owner gains 10% of its own AD, when an ally stands 2 hexes ahead-above
    ///
    /// A pattern's marks are fixed: the first is orange and pays only its self bonus, the second is green and pays only its ally bonus.
    /// One hex can be in both. A third ordinary mark does nothing (See AligningHex.ColourOf).
    ///
    /// A special mark is one the hero's skill asks for by name (SkillDefinition.SpecialMarks), so the skill can read who stands on it.
    /// The Align Editor puts it in every pattern by itself, after orange and green, and its hexes are painted like any other mark's. It gives no bonus.
    /// e.g.    Rock Golem: "Ward"              => the allies on it are shielded with him
    /// e.g.    Skeleton Archer: "Long Shot"    => his long shots deal more for every ally on it
    /// </summary>
    // AllyAmount with perOwnerAP: the ally amount per 100 of the owner's AP, e.g. 25 per 100 AP at 150 AP => 37.5.
    [Serializable]
    public struct AlignMark
    {
        [Tooltip("Every hex that gives this bonus.")]
        public List<AlignHex> hexes;

        [Tooltip("Empty = an ordinary mark. Otherwise the name the hero's skill reads this mark by. Set by the Align Editor.")]
        public string special;

        [Header("Ally bonus")]
        [Tooltip("What the ally standing there gets.")]
        public ModifierEnum modifier;

        [Tooltip("0 = the ally gets nothing.")]
        public float amount;

        [Tooltip("None = the amount is added as it is. Otherwise the amount is a percent of this stat, read from the ally's own base stat.")]
        public StatEnum percentOf;

        [Tooltip("On = the amount is per 100 of this hero's own AP, read when aligning is worked out (so at fight start). " +
                 "e.g. 25, percent of AS => +25% attack speed at 100 AP, +50% at 200 AP. Off = the amount as it is.")]
        public bool perOwnerAP;

        [Header("Self bonus")]
        [Tooltip("What this hero itself gets.")]
        public ModifierEnum selfModifier;

        [Tooltip("0 = this mark gives its owner nothing.")]
        public float selfAmount;

        [Tooltip("None = the amount is added as it is. Otherwise the amount is a percent of this stat, read from this hero's own base stat.")]
        public StatEnum selfPercentOf;

        [Tooltip("Off = the self bonus is given once for every one of this mark's hexes an ally stands on. " +
                 "On = the opposite: once for every one of this mark's deploy-zone hexes nobody stands on.")]
        public bool selfWhenEmpty;

        [Header("Generator")]
        [Tooltip("An ally standing on one of this mark's hexes is a generator: this hero heals for this percent of every damage it deals. " +
                 "0 = none.")]
        public float healFromAllyDamage;

        private const float RestOfFight = -1f;

        public bool HasAllyBonus => amount != 0f;
        public bool HasSelfBonus => selfAmount != 0f;
        public bool HasDamageHeal => healFromAllyDamage > 0f;
        public bool IsSpecial => !string.IsNullOrEmpty(special);

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

        public List<Hex> GetWearingHexes(Hex standingOn)
        {
            var wearing = new List<Hex>();
            if (hexes == null) return wearing;

            foreach (AlignHex alignHex in hexes)
            {
                if (!alignHex.isWearing) continue;

                Hex hex = alignHex.GetHex(standingOn);
                if (hex != null) wearing.Add(hex);
            }

            return wearing;
        }

        public bool HasWearingHex => hexes != null && hexes.Exists(alignHex => alignHex.isWearing);

        public ICustomModifier BuildAllyModifier(float ownerAP) => Build(modifier, AllyAmount(ownerAP), percentOf);

        public float AllyAmount(float ownerAP) => perOwnerAP ? amount * ownerAP / 100f : amount;

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
    /// e.g.    SW, 1               => the hex south-east from the hero for 1 hex
    /// e.g.    NE, 2               => the hex north-east from the hero for 2 hex
    ///
    /// A hex that is not on a straight line from the hero needs a second direction.
    /// e.g.    NE, 1, then SE, 1   => the hex north-east for 1 hex, then south-east for 1 hex
    /// </summary>
    [Serializable]
    public struct AlignHex
    {
        [Tooltip("The direction of the marked hex from this hero.")]
        public HexDirectionEnum direction;

        [Tooltip("How many hexes away in that direction.")]
        public int distance;

        [Tooltip("For a hex off the straight lines: the direction to turn to after the first direction.")]
        public HexDirectionEnum thenDirection;

        [Tooltip("How many hexes away in that then direction.")]
        public int thenDistance;

        [Tooltip("An item lying on this hex is worn by the hero. The item gives its own effect only: it is not an ally, so it gives no ally bonus and no self bonus. " +
                 "An ally standing on this hex shares the item it wears: the hero wears that too.")]
        public bool isWearing;

        public int Distance => Mathf.Max(1, distance);
        public bool HasTurn => thenDistance > 0;

        public Hex GetHex(Hex standingOn)
        {
            if (!HasTurn) return standingOn.GetNeighbor(direction, Distance);

            Hex hex = Walk(standingOn, direction, Distance, thenDirection, thenDistance);
            if (hex != null) return hex;
            return Walk(standingOn, thenDirection, thenDistance, direction, Distance);
        }

        private static Hex Walk(Hex from, HexDirectionEnum first, int firstSteps, HexDirectionEnum second, int secondSteps)
        {
            Hex corner = from.GetNeighbor(first, firstSteps);
            return corner == null ? null : corner.GetNeighbor(second, secondSteps);
        }
    }
}
