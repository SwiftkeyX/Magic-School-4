using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Align
{
    /// <summary>
    /// The aligning mechanic: a hero marks some of the hexes around it, and an ally standing on a marked hex will grant a bonus.
    /// </summary>

    // Get hexes that was marked by aligning mechanic
    // A pattern's marks are fixed by position: [0] orange pays the hero itself, [1] green pays the ally standing
    // there, then the special marks (white) pay nobody; a third ordinary mark is not used. A hex can be in several
    // marks: an ally there gets the green bonus and pays the hero the orange one (Elf: N and S are both).
    public static class AligningHex
    {
        public const int Orange = 0;
        public const int Green = 1;
        public const int Special = 2;
        public const int None = -1;

        public static List<Hex>[] MarkedHexesByBonus(Hero giver, Hex standingOn)
        {
            var hexesByBonus = new[] { new List<Hex>(), new List<Hex>(), new List<Hex>() };

            IReadOnlyList<AlignMark> marks = giver.AlignMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                int colour = ColourOf(marks, i);
                if (colour != None) hexesByBonus[colour].AddRange(marks[i].GetMarkedHexes(standingOn));
            }

            return hexesByBonus;
        }

        public static int ColourOf(IReadOnlyList<AlignMark> marks, int index)
        {
            if (marks[index].IsSpecial) return Special;
            if (index == 0) return Orange;
            if (index == 1) return Green;
            return None;
        }

        public static List<Hex> WearingHexes(Hero giver, Hex standingOn)
        {
            var wearing = new List<Hex>();

            foreach (AlignMark mark in giver.AlignMarks)
            {
                wearing.AddRange(mark.GetWearingHexes(standingOn));
            }

            return wearing;
        }

        public static List<Hex> WearingHexes(BattleBoard board, TeamEnum team)
        {
            var wearing = new List<Hex>();

            foreach (ICombatant combatant in board.HeroesOnBoard)
            {
                if (!(combatant is Hero hero) || hero.Team != team || hero.CurrentHex == null) continue;

                wearing.AddRange(WearingHexes(hero, hero.CurrentHex));
            }

            return wearing;
        }
    }
}
