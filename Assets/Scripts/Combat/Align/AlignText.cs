using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// A hero's marks, written out for the inspector. One line per bonus:
    /// e.g.    Ettin   => "+30% attack speed"
    /// e.g.    a mark with a self bonus adds "Self, per ally aligned: +10% AD"
    /// Which hexes are marked is not written: the board shows them while the hero is held.
    /// </summary>
    public static class AlignText
    {
        public static string Describe(IReadOnlyList<AlignMark> marks)
        {
            if (marks == null || marks.Count == 0) return string.Empty;

            // what an ally standing on a marked hex gets
            var allyBonus = marks
                .Where(mark => mark.HasAllyBonus)
                .Select(mark => Bonus(mark.modifier, mark.amount, mark.percentOf));

            // what the hero itself gets, once for every marked hex an ally stands on
            var selfBonus = marks
                .Where(mark => mark.HasSelfBonus)
                .Select(mark => "Self, per ally aligned: " + Bonus(mark.selfModifier, mark.selfAmount, mark.selfPercentOf));

            return string.Join("\n", allyBonus.Concat(selfBonus));
        }

        private static string Bonus(ModifierEnum modifier, float value, StatEnum percentOf)
        {
            // a percent of the hero's own stat reads as a percent; anything else is the plain number
            string amount = percentOf == StatEnum.None ? $"{value:0.#}" : $"{value:0.#}%";

            // a bonus can take away: -10 max mana
            string signed = (value < 0f ? "" : "+") + amount;

            switch (modifier)
            {
                case ModifierEnum.AS: return $"{signed} attack speed";
                case ModifierEnum.ATK: return $"{signed} AD";
                case ModifierEnum.AP: return $"{signed} AP";
                case ModifierEnum.DF: return $"{signed} DF";
                case ModifierEnum.MR: return $"{signed} MR";
                case ModifierEnum.BonusHP: return $"{signed} max HP";
                case ModifierEnum.StartMana: return $"{signed} starting mana";
                case ModifierEnum.ManaPerAttack: return $"{signed} mana per attack";
                case ModifierEnum.MaxMana: return $"{signed} max mana";
                case ModifierEnum.Range: return $"{signed} range";

                // these three are already counted in percent
                case ModifierEnum.CritChance: return $"{signed}% crit chance";
                case ModifierEnum.Omnivamp: return $"heals for {amount}% of damage dealt";
                case ModifierEnum.DamageReduction: return $"takes {amount}% less damage";

                default: return $"{signed} {modifier}";
            }
        }
    }
}
