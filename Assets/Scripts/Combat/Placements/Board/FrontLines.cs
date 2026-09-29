using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;

namespace MagicSchool.Combat.Placements
{
    // Tells every hex how many column from itself to the first enemy column: 
    // e.g.     0   =   the column facing the enemy side.
    //          1   =   the column behind 0
    internal static class FrontLines
    {
        public static void Assign(IReadOnlyDictionary<HexNumber, Hex> hexs)
        {
            var sides = hexs.GroupBy(pair => pair.Key.team).ToDictionary(side => side.Key, side => side.ToList());
            if (!sides.ContainsKey(TeamEnum.Blue) || !sides.ContainsKey(TeamEnum.Red)) return;

            float blueX = sides[TeamEnum.Blue].Average(pair => pair.Value.transform.position.x);
            float redX = sides[TeamEnum.Red].Average(pair => pair.Value.transform.position.x);

            foreach (var side in sides)
            {
                int columnCount = side.Value.Select(pair => pair.Key.column).Distinct().Count();

                // columns are numbered left to right, so a side counts toward the enemy when it sits left of it
                bool countsTowardEnemy = side.Key == TeamEnum.Blue ? blueX < redX : redX < blueX;

                foreach (var pair in side.Value)
                {
                    int column = pair.Key.column;
                    pair.Value.SetLinesFromFront(countsTowardEnemy ? columnCount - 1 - column : column);
                }
            }
        }
    }
}
