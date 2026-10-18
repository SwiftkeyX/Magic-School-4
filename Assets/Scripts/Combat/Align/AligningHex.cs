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
    public static class AligningHex
    {
        // A pattern's marks are fixed: the first is orange, the second green, and the special marks after them are white (See AlignMark)
        public const int Orange = 0;    // pays the hero itself
        public const int Green = 1;     // pays the ally standing there
        public const int Special = 2;   // pays nobody: the skill reads who stands there
        public const int None = -1;     // a third ordinary mark: not used

        // Get the marked hexes of the specify hero as three lists, one per colour the board paints them.
        // A hex can be in more than one: an ally there gets the green bonus and pays the hero the orange one.
        // e.g.     Elf: N and S                    => [0] = N hex, S hex     [1] = N hex, S hex    [2] = empty
        // e.g.     Rock Golem: his Ward mark on N  => [0] = empty            [1] = empty           [2] = N hex
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

        // Which colour a mark is: a special mark is white, the first mark orange, the second green
        public static int ColourOf(IReadOnlyList<AlignMark> marks, int index)
        {
            if (marks[index].IsSpecial) return Special;
            if (index == 0) return Orange;
            if (index == 1) return Green;
            return None;
        }

        // Get the wearing hexes of the specify hero: the hexes where an item would be worn by it.
        public static List<Hex> WearingHexes(Hero giver, Hex standingOn)
        {
            var wearing = new List<Hex>();

            foreach (AlignMark mark in giver.AlignMarks)
            {
                wearing.AddRange(mark.GetWearingHexes(standingOn));
            }

            return wearing;
        }

        // Get every wearing hex of the consuming team: the hexes where an item would be worn by someone.
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
