using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Some skill land on a cluster of enemies. This is the logic to find the most cluster of enemies.
    // In the game, there're 3 clustered: 
    // 1) clustered circle - which spot caught the most enemies with specify radius
    // 2) clustered laser  - which spot caught the most enemies with this laser width (laser are straight line hitbox that hit everything in the path)
    // 3) clustered charge - which spot caught the most enemies when I charge into them (similar to laser but different nuisance)   
    internal static class ClusterAim
    {
        // ========================================= public =========================================
        // From candidate hex, which one puts the most enemies inside blastRadius?
        public static Hex BestCircle(Vector3 me, IReadOnlyList<Hex> candidates, IReadOnlyList<ICombatant> enemies, float blastRadius)
        {
            Func<Hex, int> caught = hex => CountWithin(enemies, hex.transform.position, blastRadius);

            return BestHex(me, candidates, caught);
        }

        // From candidate hex (landings), which one hit the most enemies on the way to landing spot?
        public static Hex BestCharge(Vector3 me, IReadOnlyList<Hex> landings, IReadOnlyList<ICombatant> enemies, float chargeHalfWidth)
        {
            Func<Hex, int> caught = hex => CountSwept(enemies, me, hex.transform.position, chargeHalfWidth);

            return BestHex(me, landings, caught);
        }

        // From candidate enemies, which one that my beam going to hit the most enemies? 
        public static List<ICombatant> BestLaserAims(Vector3 me, IReadOnlyList<ICombatant> enemies, int reachRange, float beamHalfWidth, out int caught)
        {
            List<ICombatant> best = new List<ICombatant>();
            int bestCount = -1;

            // aim at each enemy, and count how many hero caught in the beam
            foreach (ICombatant candidate in enemies)
            {
                Vector3 toCandidate = candidate.CurrentHex().transform.position - me;
                float candidateDistance = toCandidate.magnitude;

                if (candidateDistance <= Mathf.Epsilon) continue;

                Vector3 direction = toCandidate / candidateDistance;
                Vector3 end = me + direction * reachRange;
                int count = CountSwept(enemies, me, end, beamHalfWidth, except: candidate);

                // found better candidate
                if (count > bestCount) { bestCount = count; best.Clear(); }

                // keep every candidate that ties for best
                if (count == bestCount) best.Add(candidate);
            }

            caught = bestCount;
            return best;
        }

        // ========================================= private =========================================
        // From candidate hex, pick the one that catches the most heroes
        private static Hex BestHex(Vector3 me, IReadOnlyList<Hex> candidates, Func<Hex, int> countCaughtFrom)
        {
            if (candidates == null) return null;

            Hex best = null;
            int bestCount = 0;
            float bestDistance = 0f;

            foreach (Hex candidate in candidates)
            {
                int count = countCaughtFrom(candidate);
                if (count == 0) continue;

                // prefered the one with the less distance from me
                float distance = Vector3.Distance(me, candidate.transform.position);
                if (count < bestCount) continue;
                if (count == bestCount && distance >= bestDistance) continue;

                best = candidate;
                bestCount = count;
                bestDistance = distance;
            }

            return best;
        }

        // How many enemy hit with in the radius?
        private static int CountWithin(IReadOnlyList<ICombatant> enemies, Vector3 centre, float radius)
        {
            return enemies.Count(enemy => Vector3.Distance(centre, enemy.CurrentHex().transform.position) <= radius);
        }

        // How many enemy hit by the [x] width laser?
        private static int CountSwept(IReadOnlyList<ICombatant> enemies, Vector3 from, Vector3 to, float halfWidth, ICombatant except = null)
        {
            return enemies.Count(enemy => enemy != except && IsSweptOnTheWay(from, to, enemy, halfWidth));
        }

        // Is this enemy close enough to the path of a laser/charge to be hit by it?
        private static bool IsSweptOnTheWay(Vector3 from, Vector3 to, ICombatant enemy, float halfWidth)
        {
            Hex enemyHex = enemy.CurrentHex();
            if (enemyHex == null) return false;

            Vector3 path = to - from;
            float pathLength = path.magnitude;
            if (pathLength <= Mathf.Epsilon) return false;

            Vector3 direction = path / pathLength;
            Vector3 toEnemy = enemyHex.transform.position - from;

            // how far along the charge the enemy stands - behind is not swept, past the end counts
            // as being at the end, which is where the caster comes to a stop on top of them
            float along = Vector3.Dot(toEnemy, direction);
            if (along <= 0f) return false;

            along = Mathf.Min(along, pathLength);

            return (toEnemy - direction * along).magnitude <= halfWidth;
        }
    }
}
