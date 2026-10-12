using System;
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
    /// Three things that was used in aligning mechanic.
    ///     1. A hex that was marked by the aligning mechanic
    ///     2. A modifier that was granted when aligned
    ///     3. 
    /// e.g.    Ettin: SW, AS, 30, percent of AS    => the hero behind-below him gains 30% of its own attack speed
    /// e.g.    Centaur: SW, BonusHP, 200, None     => the hero behind-below him gains 200 HP
    /// (See Aligning.cs)
    /// </summary>
    [Serializable]
    public struct AlignMark
    {
        [Tooltip("Which of the 6 hexes around this hero is marked.")]
        public HexDirectionEnum direction;

        [Tooltip("What the ally standing there gets.")]
        public ModifierEnum modifier;

        public float amount;

        [Tooltip("None = the amount is added as it is. Otherwise the amount is a percent of this stat, read from the ally's own base stat.")]
        public StatEnum percentOf;

        private const float RestOfFight = -1f;

        // The bonus as a modifier, which lasts until the hero's stats are reset for the next stage.
        public ICustomModifier BuildModifier()
        {
            StatRatio ratio = percentOf == StatEnum.None
                ? new StatRatio(amount)
                : new StatRatio(percentOf, amount, ScaleFromEnum.Base);

            IModifier bonus = new StatModifier(modifier, new[] { ratio }, ScalingSourceEnum.Recipient);

            return new CustomModifier(RestOfFight, new[] { bonus }, isStack: true);
        }
    }
}
