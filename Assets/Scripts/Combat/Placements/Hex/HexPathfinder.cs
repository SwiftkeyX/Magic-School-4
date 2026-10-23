using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    // Finds the next step along the shortest unoccupied route between hexes.
    internal static class HexPathfinder
    {
        // How close two distances have to be to count as the same
        private const float TieEpsilon = 0.01f;

        // Returns ONE step from startHex toward any hex adjacent to targetHex, not the whole route.
        // Null means the target is fully boxed in - no free hex adjacent to it - or there is no way there.
        //
        // isHexBlocked: is this hex claimed by someone other than the mover? Hex doesn't track
        // occupancy itself, so the caller owns that.
        //
        // Often several first steps start an equally short route (round the top or the bottom of an ally).
        // They are told apart on purpose, never by the order a hex lists its neighbours - that order is top first,
        // which once made every hero go over the top, and so pick the enemy above:
        //      1) the step nearer the target
        //      2) lean: +1 the upper step, -1 the lower one - the mover's side for the fight (See Hero.PathLean)
        //
        // The step returned is the real one even when it doesn't look like progress: routing around
        // a blocked hex can start sideways or backward. Whether to take it now or wait for the
        // contention to clear is timing, not pathfinding, so that's the caller's call.
        public static Hex FindValidHexToTarget(Hex startHex, Hex targetHex, Func<Hex, bool> isHexBlocked, int lean)
        {
            // find empty hex from the neighbors. (empty hex = hex that no hero reserved it)
            var goalHexes = targetHex.GetNeighbors().Where(h => !isHexBlocked(h)).ToList();
            if (goalHexes.Count == 0) return null;

            // steps from every reachable hex to the nearest goal, counted outward from the goals
            Dictionary<Hex, int> stepsToGoal = StepsToGoal(goalHexes, isHexBlocked);

            if (!stepsToGoal.TryGetValue(startHex, out int mySteps) || mySteps == 0) return null;

            // every neighbour one step closer starts a shortest route
            List<Hex> firstSteps = startHex.GetNeighbors()
                .Where(n => stepsToGoal.TryGetValue(n, out int steps) && steps == mySteps - 1)
                .ToList();

            return BreakTie(firstSteps, targetHex, lean);
        }

        // breadth-first from the goals through free hexes
        private static Dictionary<Hex, int> StepsToGoal(List<Hex> goalHexes, Func<Hex, bool> isHexBlocked)
        {
            var steps = new Dictionary<Hex, int>();
            var frontier = new Queue<Hex>();

            foreach (Hex goal in goalHexes)
            {
                steps[goal] = 0;
                frontier.Enqueue(goal);
            }

            while (frontier.Count > 0)
            {
                Hex current = frontier.Dequeue();

                foreach (Hex neighbor in current.GetNeighbors())
                {
                    if (steps.ContainsKey(neighbor) || isHexBlocked(neighbor)) continue;

                    steps[neighbor] = steps[current] + 1;
                    frontier.Enqueue(neighbor);
                }
            }

            return steps;
        }

        private static Hex BreakTie(List<Hex> steps, Hex targetHex, int lean)
        {
            if (steps.Count <= 1) return steps.FirstOrDefault();

            // 1) nearer the target
            Vector3 target = targetHex.transform.position;
            float nearest = steps.Min(h => Vector3.Distance(h.transform.position, target));
            steps = steps.Where(h => Vector3.Distance(h.transform.position, target) <= nearest + TieEpsilon).ToList();

            if (steps.Count == 1) return steps[0];

            // 2) the mover's lean
            return steps.OrderByDescending(h => h.transform.position.y * lean).First();
        }
    }
}
