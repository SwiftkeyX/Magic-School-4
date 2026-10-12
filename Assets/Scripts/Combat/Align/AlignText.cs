using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// A hero's marks, written out for the inspector. One line per bonus:
    /// e.g.    Ettin   => "+30% attack speed"
    /// Which hexes are marked is not written: the board shows them while the hero is held.
    /// </summary>
    public static class AlignText
    {
        public static string Describe(IReadOnlyList<AlignMark> marks)
        {
            if (marks == null || marks.Count == 0) return string.Empty;

            // marks that give the same thing share a line
            var lines = marks
                .GroupBy(mark => (mark.modifier, mark.amount, mark.percentOf))
                .Select(group => Bonus(group.First()));

            return string.Join("\n", lines);
        }

        private static string Bonus(AlignMark mark)
        {
            // a percent of the ally's own stat reads as a percent; anything else is the plain number
            string amount = mark.percentOf == StatEnum.None ? $"{mark.amount:0.#}" : $"{mark.amount:0.#}%";

            switch (mark.modifier)
            {
                case ModifierEnum.AS: return $"+{amount} attack speed";
                case ModifierEnum.ATK: return $"+{amount} AD";
                case ModifierEnum.AP: return $"+{amount} AP";
                case ModifierEnum.DF: return $"+{amount} DF";
                case ModifierEnum.MR: return $"+{amount} MR";
                case ModifierEnum.BonusHP: return $"+{amount} max HP";
                case ModifierEnum.StartMana: return $"+{amount} starting mana";
                case ModifierEnum.ManaPerAttack: return $"+{amount} mana per attack";
                case ModifierEnum.Range: return $"+{amount} range";

                // these three are already counted in percent
                case ModifierEnum.CritChance: return $"+{amount}% crit chance";
                case ModifierEnum.Omnivamp: return $"heals for {amount}% of damage dealt";
                case ModifierEnum.DamageReduction: return $"takes {amount}% less damage";

                default: return $"+{amount} {mark.modifier}";
            }
        }
    }
}
