using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Heroes need a way to find a enemies it want to hit, and find where does the skill should land 
    // FindTarget have 4 category:
    // 1) CurrentTarget - the [combatant] that me was currently attacking (hero won't stop attack current target, until something break them off)
    // 2) NearestEnemy - the [combatant] that was closest to me (hero will always find next nearest enemy as next current target)
    // 3) FurthestEnemy - the [combatant] that was furthest to me in distance NOT hop (some skill prioritize the furthest enemy)
    // 4) Clustered - the [placement/combatant] that hit the most combatant with specify skill's [radius/width].
    // 5) EnemiesNear - the [combatant] in [x] hop of the specify target
    internal class FindTarget
    {
        private const float TieEpsilon = 0.01f;     // How close two enemies' distances have to be to count as tied.

        private readonly Hero _me;
        private readonly EnemyScan _scan;

        private ICombatant _lastNearest;
        private ICombatant _currentTarget;
        internal ICombatant CurrentTarget => _currentTarget;

        public FindTarget(Hero me, BattleBoard board)
        {
            _me = me;
            _scan = new EnemyScan(me, board);
        }

        // ========================================= public =========================================

        // When a hero auto attack a enemy, he'll pick that enemy as current target.
        // Which mean he'll stick to that target until something break them off.
        // read IsStillEngageWith() for more detail.
        public ICombatant FindCurrentTarget()
        {
            if (IsStillEngagedWith(_currentTarget)) return _currentTarget;

            // if current target is break off, find new target.
            ICombatant fresh = FindNearestEnemy();
            _currentTarget = fresh;

            return fresh;
        }

        // Picks the enemy that costs the fewest steps to walk to
        // consider step as hop amount
        public ICombatant FindNearestEnemy()
        {
            List<(ICombatant target, int steps)> enemySteps = _scan.Steps();

            if (enemySteps.Count == 0) return null;

            int nearestSteps = enemySteps.Min(e => e.steps);
            var tiedNearest = enemySteps.Where(e => e.steps == nearestSteps).Select(e => e.target).ToList();

            // if previous answer exist, use the previous one
            // if no exist, use new target
            ICombatant preferred = _lastNearest ?? _currentTarget;

            // read function comment
            ICombatant nearestEnemy = BreakTie(tiedNearest, preferred);
            _lastNearest = nearestEnemy;

            return nearestEnemy;
        }

        // Picks furthest enemy within reachRange
        public ICombatant FindFurthestEnemy(int reachRange)
        {
            var enemyDistances = _scan.Distances().Where(e => e.dist <= reachRange).ToList();

            if (enemyDistances.Count == 0) return null;

            float furthestDist = enemyDistances.Max(e => e.dist);
            var tiedFurthest = enemyDistances.Where(e => e.dist >= furthestDist - TieEpsilon).Select(e => e.target).ToList();

            return BreakTie(tiedFurthest, _currentTarget);
        }

        // Pick a placement where:
        // 1) a blastRadius hit most enemies
        // 2) placement is within my reach range.
        // 3) if this is a jump, a placement is also need to be free, for me to land
        public IPlacement FindClusteredCircle(int reachRange, float blastRadius, bool isJump)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<Hex> candidates = null;

            // if this was a jump, the candidates could only land on free hex.
            // free hex = hex where no one standing on
            if (isJump)
            {
                candidates = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther);
            }

            // if not a jump, the candidates hex could land on any hex with in reach range.
            else
            {
                candidates = HexFinder.FindHexesWithin(myHex, reachRange);
            }

            // guard
            if (candidates == null || candidates.Count == 0) return null;

            return ClusterAim.BestCircle(myHex.transform.position, candidates, enemies, blastRadius);
        }

        // Context: the caster is about to charge in a straight line, with a hitbox riding on him.
        // Pick the placement where caster'll be landing. And hit the most enemies along the way.
        public IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            // standing still is not a charge - there would be no path to sweep
            List<Hex> landings = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther);

            return ClusterAim.BestCharge(myHex.transform.position, landings, enemies, chargeHalfWidth);
        }

        // Context: the laser will be shoot from caster to a enemy.
        // Pick the enemy that a laser'll go through the most enemies.
        public ICombatant FindClusteredLaser(int reachRange, float beamHalfWidth)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<ICombatant> bestAims = ClusterAim.BestLaserAims(myHex.transform.position, enemies, reachRange, beamHalfWidth, out int caught);

            // the laser can't hit more than 1 target, so just shoot the target we have
            bool noOtherEnemyBesideTarget = caught <= 0;
            if (noOtherEnemyBesideTarget) return UseCurrentTargetOrNewTarget();

            return BreakTie(bestAims, _currentTarget);
        }

        // Find all enemies within reachRange of target's hex
        public IReadOnlyList<ICombatant> FindEnemiesNear(ICombatant target, int reachRange)
        {
            Hex targetHex = target?.CurrentHex();
            if (targetHex == null) return Array.Empty<ICombatant>();

            HashSet<Hex> nearby = new HashSet<Hex>(HexFinder.FindHexesWithin(targetHex, reachRange));

            return _scan.GetAllEnemy().Where(enemy => enemy.CurrentHex() != null && nearby.Contains(enemy.CurrentHex())).ToList();
        }


        // ========================================= private =========================================
        // when choosing a target, it could have several best candidate (a tied)
        // This is the protocol to solve when there's a tied target.
        // FLAGGING: The breaktie() logic for FindEnemy should be reconsider again. since the FindClustered doesn't hold the same logic anymore.
        private ICombatant BreakTie(List<ICombatant> tied, ICombatant preferred)
        {
            if (tied.Count == 1) return tied[0];

            // 1) if the tied list have prefered target, choose prefered target
            // context: prefered target = me's currentTarget
            if (preferred != null && preferred.IsAlive && tied.Contains(preferred)) return preferred;

            // 2) choose the nearest target in the tied
            Hex myHex = _me.CurrentHex;
            if (myHex != null)
            {
                Vector3 origin = myHex.transform.position;

                var withDistance = tied
                    .Where(candidate => candidate.CurrentHex() != null)
                    .Select(candidate => (candidate, dist: Vector3.Distance(origin, candidate.CurrentHex().transform.position)))
                    .ToList();

                if (withDistance.Count > 0)
                {
                    float nearestDist = withDistance.Min(c => c.dist);
                    tied = withDistance.Where(c => c.dist <= nearestDist + TieEpsilon).Select(c => c.candidate).ToList();

                    if (tied.Count == 1) return tied[0];
                }
            }

            // 3) if nothing work, random between the tied
            return tied[UnityEngine.Random.Range(0, tied.Count)];   // spelt out: `using System` makes a bare Random ambiguous
        }

        // A twin to FindCurrentTarget()
        // the difference is this function, don't set new target which is desirable.
        private ICombatant UseCurrentTargetOrNewTarget()
        {
            ICombatant engaged = _currentTarget;

            if (engaged != null && engaged.IsAlive && engaged.IsInCombat) return engaged;

            return FindNearestEnemy();
        }

        // Hero'll pick new current target if:
        // 1) If target is out of range, pick new target.
        // 2) If target is dead, pick new target.
        private bool IsStillEngagedWith(ICombatant engaged)
        {
            // Is current target is dead?
            if (engaged == null || !engaged.IsAlive || !engaged.IsInCombat) return false;

            Hex myHex = _me.CurrentHex;
            if (myHex == null || engaged.CurrentHex() == null) return false;

            // Is current target is out of range?
            return myHex.IsWithinRange(engaged.CurrentHex(), _me.Range);
        }
    }
}
