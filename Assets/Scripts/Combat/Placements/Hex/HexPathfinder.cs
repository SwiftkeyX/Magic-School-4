using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicSchool.Combat.Placements
{
    // Finds the next step along the shortest unoccupied route between hexes.
    // Returns ONE step from the start toward a free hex next to the target, not the whole route; null when the target
    // is boxed in or there is no way there. Steps are counted breadth-first outward from those goal hexes through free
    // hexes. isHexBlocked is the caller's: a hex does not track who stands on it.
    // Equally short first steps (round the top or the bottom of an ally) are told apart on purpose, never by the order
    // a hex lists its neighbours - top first, which once sent every hero over the top and so to the enemy above: 1)
    // the step nearer the target, 2) the mover's lean (+1 upper, -1 lower, See Hero.PathLean).
    // The step returned is the real one even when it does not look like progress: routing round a blocked hex can
    // start sideways or backward. Whether to take it or wait is timing, the caller's call.
    internal static class HexPathfinder
    {
        private const float TieEpsilon = 0.01f;

        public static Hex FindValidHexToTarget(Hex startHex, Hex targetHex, Func<Hex, bool> isHexBlocked, int lean)
        {
            var goalHexes = targetHex.GetNeighbors().Where(h => !isHexBlocked(h)).ToList();
            if (goalHexes.Count == 0) return null;

            Dictionary<Hex, int> stepsToGoal = StepsToGoal(goalHexes, isHexBlocked);

            if (!stepsToGoal.TryGetValue(startHex, out int mySteps) || mySteps == 0) return null;

            List<Hex> firstSteps = startHex.GetNeighbors()
                .Where(n => stepsToGoal.TryGetValue(n, out int steps) && steps == mySteps - 1)
                .ToList();

            return BreakTie(firstSteps, targetHex, lean);
        }

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

            Vector3 target = targetHex.transform.position;
            float nearest = steps.Min(h => Vector3.Distance(h.transform.position, target));
            steps = steps.Where(h => Vector3.Distance(h.transform.position, target) <= nearest + TieEpsilon).ToList();

            if (steps.Count == 1) return steps[0];

            return steps.OrderByDescending(h => h.transform.position.y * lean).First();
        }
    }
}
