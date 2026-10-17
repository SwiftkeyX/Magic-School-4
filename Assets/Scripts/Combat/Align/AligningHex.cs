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
        public const int FirstBonus = 0;
        public const int SecondBonus = 1;
        public const int Special = 2;

        // Get the marked hexes of the specify hero as three lists, one per colour the board paints them:
        // e.g.     Elf: N and S give AP                    => [0] = N hex, S hex     [1] = empty     [2] = empty
        // e.g.     Random Hero: N gives AP, S gives AD     => [0] = N hex            [1] = S hex     [2] = empty
        // e.g.     Rock Golem: his Ward mark on N          => [0] = empty            [1] = empty     [2] = N hex
        public static List<Hex>[] MarkedHexesByBonus(Hero giver, Hex standingOn)
        {
            var hexesByBonus = new[] { new List<Hex>(), new List<Hex>(), new List<Hex>() };

            IReadOnlyList<AlignMark> marks = giver.AlignMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                hexesByBonus[ColourOf(marks, i)].AddRange(marks[i].GetMarkedHexes(standingOn));
            }

            return hexesByBonus;
        }

        // Which colour a mark is painted: a special mark has its own, and the ordinary marks are counted without them -
        // the second ordinary mark is the second bonus, and a third is painted like the first.
        public static int ColourOf(IReadOnlyList<AlignMark> marks, int index)
        {
            if (marks[index].IsSpecial) return Special;

            int ordinary = 0;
            for (int i = 0; i < index; i++)
            {
                if (!marks[i].IsSpecial) ordinary++;
            }

            return ordinary == 1 ? SecondBonus : FirstBonus;
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
