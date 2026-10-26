using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// A hero's marks, written out for the inspector. One line per colour, saying what that colour does:
    /// e.g.    Ettin   => "Green: an ally here gets +30% attack speed"
    /// e.g.    Elf     => "Orange: for every ally here, she gets -10 max mana" and "Green: an ally here gets +30 AP"
    /// Every bonus is written, even one with no hexes in the pattern the hero is turned to: the bonuses are the hero's,
    /// the same in every pattern, and turning it may bring them back. Which hexes are marked is not written:
    /// the board shows them, in the same colours, while the hero is held.
    /// </summary>
    // A bonus that scales with the owner's AP is written as the number for its AP now, or with no AP (Alt held) as its
    // formula: "+46.3% attack speed" or "+[25% AP]% attack speed". A percent of the hero's own stat reads as a
    // percent; anything else as the plain number.
    public static class AlignText
    {
        public static string Describe(IReadOnlyList<AlignMark> marks, int pattern, int patternCount, float? ownerAP = null)
        {
            string text = Describe(marks, ownerAP);
            if (patternCount <= 1) return text;

            string turned = $"Pattern {pattern + 1} of {patternCount} (right-click while holding to rotate)";
            return string.IsNullOrEmpty(text) ? turned : text + "\n" + turned;
        }

        public static string Describe(IReadOnlyList<AlignMark> marks, float? ownerAP = null)
        {
            if (marks == null || marks.Count == 0) return string.Empty;

            var lines = new List<string>();

            for (int i = 0; i < marks.Count; i++)
            {
                AlignMark mark = marks[i];

                switch (AligningHex.ColourOf(marks, i))
                {
                    case AligningHex.Orange:
                        if (mark.HasSelfBonus)
                        {
                            string per = mark.selfWhenEmpty ? "for every hex here left empty" : "for every ally here";
                            lines.Add($"Orange: {per}, this hero {Gets(Bonus(mark.selfModifier, mark.selfAmount, mark.selfPercentOf))}");
                        }

                        if (mark.HasDamageHeal)
                            lines.Add($"Orange: this hero heals for {mark.healFromAllyDamage:0.#}% of the damage an ally here deals");
                        break;

                    case AligningHex.Green:
                        if (mark.HasAllyBonus) lines.Add($"Green: an ally here {Gets(AllyBonus(mark, ownerAP))}");
                        break;

                    case AligningHex.Special:
                        lines.Add($"White ({mark.special}): the skill counts the allies here");
                        break;
                }
            }

            if (marks.Any(mark => mark.HasWearingHex)) lines.Add("Purple: this hero wears the item lying here, or the item of an ally standing here");

            return string.Join("\n", lines);
        }

        private static string AllyBonus(AlignMark mark, float? ownerAP)
        {
            if (!mark.perOwnerAP) return Bonus(mark.modifier, mark.amount, mark.percentOf);

            return ownerAP.HasValue
                ? Bonus(mark.modifier, mark.AllyAmount(ownerAP.Value), mark.percentOf)
                : Bonus(mark.modifier, mark.amount, mark.percentOf, formulaOf: "AP");
        }

        private static string Gets(string bonus)
            => bonus.StartsWith("takes ") || bonus.StartsWith("heals ") ? bonus : "gets " + bonus;

        private static string Bonus(ModifierEnum modifier, float value, StatEnum percentOf, string formulaOf = null)
        {
            string number = formulaOf == null ? $"{value:0.#}" : $"[{value:0.#}% {formulaOf}]";
            string amount = percentOf == StatEnum.None ? number : number + "%";

            string signed = (value < 0f ? "" : "+") + amount;

            switch (modifier)
            {
                case ModifierEnum.AS: return $"{signed} attack speed";
                case ModifierEnum.ATK: return $"{signed} AD";
                case ModifierEnum.AP: return $"{signed} AP";
                case ModifierEnum.DF: return $"{signed} DF";
                case ModifierEnum.BonusHP: return $"{signed} max HP";
                case ModifierEnum.StartMana: return $"{signed} starting mana";
                case ModifierEnum.ManaPerAttack: return $"{signed} mana per attack";
                case ModifierEnum.ManaPerSecond: return $"{signed} mana per second";
                case ModifierEnum.HPPerSecond: return $"{signed} HP per second";
                case ModifierEnum.Lethality: return $"{signed} lethality";
                case ModifierEnum.MaxMana: return $"{signed} max mana";
                case ModifierEnum.Range: return $"{signed} range";

                case ModifierEnum.CritChance: return $"{signed}% crit chance";
                case ModifierEnum.Omnivamp: return $"heals for {amount}% of damage dealt";
                case ModifierEnum.DamageReduction: return $"takes {amount}% less damage";
                case ModifierEnum.ArmorPen: return $"{signed}% armor penetration";

                default: return $"{signed} {modifier}";
            }
        }
    }
}
