using System;
using System.Collections.Generic;
using System.Linq;

namespace MagicSchool.Combat.Placements
{
    // Picks a hex for a specify condition
    // Other class need to find a hex for some reason e.g. a jump, a dash, a summon. 
    internal static class HexFinder
    {
        public static List<Hex> FindFreeHexesWithin(Hex from, int range, Func<Hex, bool> isHexBlocked)
            => FindHexesWithin(from, range).Where(hex => !isHexBlocked(hex)).ToList();

        public static Dictionary<Hex, int> StepsFrom(Hex from, Func<Hex, bool> isHexBlocked)
        {
            var steps = new Dictionary<Hex, int>();
            if (from == null) return steps;

            steps[from] = 0;
            var currentPossibleHex = new List<Hex> { from };

            while (currentPossibleHex.Count > 0)
            {
                var nextPossibleHex = new List<Hex>();
                foreach (Hex hex in currentPossibleHex)
                {
                    foreach (Hex neighbor in hex.GetNeighbors())
                    {
                        if (steps.ContainsKey(neighbor)) continue;

                        if (isHexBlocked != null && isHexBlocked(neighbor)) continue;

                        int hop = steps[hex] + 1;
                        steps[neighbor] = hop;

                        nextPossibleHex.Add(neighbor);
                    }
                }

                currentPossibleHex = nextPossibleHex;
            }

            return steps;
        }

        public static List<Hex> FindHexesWithin(Hex centre, int radius)
        {
            var inReach = new List<Hex>();
            if (centre == null) return inReach;

            inReach.Add(centre);

            var visited = new HashSet<Hex> { centre };
            var frontier = new List<Hex> { centre };

            for (int step = 0; step < radius; step++)
            {
                if (frontier.Count == 0) break;

                var nextFrontier = new List<Hex>();
                foreach (Hex hex in frontier)
                {
                    foreach (Hex neighbor in hex.GetNeighbors())
                    {
                        if (!visited.Add(neighbor)) continue;

                        nextFrontier.Add(neighbor);
                        inReach.Add(neighbor);
                    }
                }
                frontier = nextFrontier;
            }

            return inReach;
        }
    }
}
