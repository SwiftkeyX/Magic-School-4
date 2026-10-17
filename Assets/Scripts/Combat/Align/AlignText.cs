using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// A hero's marks, written out for the inspector. One line per colour, saying what that colour does:
    /// e.g.    Ettin   => "Green: an ally here gets +30% attack speed"
    /// e.g.    Elf     => "Orange: for every ally here, she gets -10 max mana" and "Green: an ally here gets +30 AP"
    /// A mark with no hexes in this pattern is not written. Which hexes are marked is not written either:
    /// the board shows them, in the same colours, while the hero is held.
    /// </summary>
    public static class AlignText
    {
        // the marks of the pattern a hero is turned to, and which pattern that is when it has more than one
        // e.g.    "+30% attack speed\nPattern 2 of 6 (right-click while holding to rotate)"
        public static string Describe(IReadOnlyList<AlignMark> marks, int pattern, int patternCount)
        {
            string text = Describe(marks);
            if (patternCount <= 1) return text;

            string turned = $"Pattern {pattern + 1} of {patternCount} (right-click while holding to rotate)";
            return string.IsNullOrEmpty(text) ? turned : text + "\n" + turned;
        }

        public static string Describe(IReadOnlyList<AlignMark> marks)
        {
            if (marks == null || marks.Count == 0) return string.Empty;

            var lines = new List<string>();

            for (int i = 0; i < marks.Count; i++)
            {
                AlignMark mark = marks[i];
                if (mark.hexes == null || mark.hexes.Count == 0) continue;

                switch (AligningHex.ColourOf(marks, i))
                {
                    // orange pays the hero itself: once for every ally standing on it, or for every hex of it left empty
                    case AligningHex.Orange:
                        if (mark.HasSelfBonus)
                        {
                            string per = mark.selfWhenEmpty ? "for every hex here left empty" : "for every ally here";
                            lines.Add($"Orange: {per}, this hero {Gets(Bonus(mark.selfModifier, mark.selfAmount, mark.selfPercentOf))}");
                        }

                        // an ally standing on it is a generator: its damage heals the hero
                        if (mark.HasDamageHeal)
                            lines.Add($"Orange: this hero heals for {mark.healFromAllyDamage:0.#}% of the damage an ally here deals");
                        break;

                    // green pays the ally standing on it
                    case AligningHex.Green:
                        if (mark.HasAllyBonus) lines.Add($"Green: an ally here {Gets(Bonus(mark.modifier, mark.amount, mark.percentOf))}");
                        break;

                    // white pays nobody: the skill reads who stands on it (the skill's own text says what for)
                    case AligningHex.Special:
                        lines.Add($"White ({mark.special}): the skill counts the allies here");
                        break;
                }
            }

            // an item lying on a wearing hex, or worn by an ally standing there, is worn by the hero
            if (marks.Any(mark => mark.HasWearingHex)) lines.Add("Purple: this hero wears the item lying here, or the item of an ally standing here");

            return string.Join("\n", lines);
        }

        // "+30 AP" reads after "gets"; "takes 15% less damage" and "heals for ..." already say what happens
        private static string Gets(string bonus)
            => bonus.StartsWith("takes ") || bonus.StartsWith("heals ") ? bonus : "gets " + bonus;

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
