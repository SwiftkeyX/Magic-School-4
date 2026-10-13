using System.Collections.Generic;
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
        // Get the marked hexes of the specify hero as two lists.
        // Each one give different bonus:
        // e.g.     Elf: N and S give AP                    => [0] = N hex, S hex     [1] = empty
        // e.g.     Random Hero: N gives AP, S gives AD     => [0] = N hex            [1] = S hex
        public static List<Hex>[] MarkedHexesByBonus(Hero giver, Hex standingOn)
        {
            var hexesByBonus = new[] { new List<Hex>(), new List<Hex>() };

            IReadOnlyList<AlignMark> marks = giver.Data.AlignMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                int bonus = i == 1 ? 1 : 0;
                hexesByBonus[bonus].AddRange(marks[i].GetMarkedHexes(standingOn));
            }

            return hexesByBonus;
        }
    }
}
