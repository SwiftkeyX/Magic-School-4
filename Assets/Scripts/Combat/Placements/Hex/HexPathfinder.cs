using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    // Finds the next step along the shortest unoccupied route between hexes, via A* search.
    internal static class HexPathfinder
    {
        // The largest possible Euclidean distance between two adjacent hexes (matches the
        // 1.15x neighbor threshold in Hex.cs). Dividing the heuristic by this keeps it
        // admissible - it never overestimates the true number of hex-hops remaining.
        private const float MaxHexStepDistance = 1.15f;

        // Returns ONE step from startHex toward any hex adjacent to targetHex, not the whole route.
        // Null means the target is fully boxed in - no free hex adjacent to it.
        //
        // isHexBlocked: is this hex claimed by someone other than the mover? Hex doesn't track
        // occupancy itself, so the caller owns that.
        //
        // A* rather than BFS so equally-short routes tie-break toward the target, instead of by
        // whichever was found first - which can double back through the mover's own side.
        //
        // The step returned is the real one even when it doesn't look like progress: routing around
        // a blocked hex can start sideways or backward. Whether to take it now or wait for the
        // contention to clear is timing, not pathfinding, so that's the caller's call.
        public static Hex FindValidHexToTarget(Hex startHex, Hex targetHex, Func<Hex, bool> isHexBlocked)
        {
            // find empty hex from the neighbors. (empty hex = hex that no hero reserved it) 
            var goalHexes = new HashSet<Hex>(targetHex.GetNeighbors().Where(h => !isHexBlocked(h)));
            if (goalHexes.Count == 0) return null;

            float Heuristic(Hex h) => goalHexes.Min(g => Vector3.Distance(h.transform.position, g.transform.position)) / MaxHexStepDistance;

            var cameFrom = new Dictionary<Hex, Hex>();
            var gScore = new Dictionary<Hex, int> { [startHex] = 0 };
            var open = new List<Hex> { startHex };
            var closed = new HashSet<Hex>();

            while (open.Count > 0)
            {
                open.Sort((a, b) => (gScore[a] + Heuristic(a)).CompareTo(gScore[b] + Heuristic(b)));
                Hex current = open[0];
                open.RemoveAt(0);

                if (goalHexes.Contains(current))
                {
                    // Walk the backtrack chain to the step right after startHex.
                    Hex step = current;
                    while (cameFrom[step] != startHex) step = cameFrom[step];
                    return step;
                }

                closed.Add(current);

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (closed.Contains(neighbor) || isHexBlocked(neighbor)) continue;

                    int tentativeG = gScore[current] + 1;
                    if (gScore.TryGetValue(neighbor, out int existingG) && tentativeG >= existingG) continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }

            return null;
        }
    }
}
