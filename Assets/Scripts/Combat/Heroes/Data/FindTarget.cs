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
    // 3) FurthestEnemy - the [combatant] that was furthest to me in hexes (some skill prioritize the furthest enemy)
    // 4) Clustered - the [placement/combatant] that hit the most combatant with specify skill's [radius/width].
    // 5) EnemiesNear - the [combatant] in [x] hop of the specify target
    // A hero sticks to the enemy it auto attacks until it dies, vanishes or leaves range. Nearest is the fewest steps
    // until in range, an enemy already in range first. On a tie the player may pick (SwitchChosenTarget steps through
    // the tied enemies, left first then top, and back to automatic); the pick is sticky in the fight while it can be
    // walked to, unless another enemy is already in range, and at fight start counts only if still tied.
    // Furthest is counted in hexes and equal ones are shuffled: in world distance a diagonal hex always beat a
    // straight one, so of two enemies 5 hexes away the same one won every time. Ties otherwise go to the current
    // target, then the nearest, then random. A charge landing that sweeps as many enemies takes the closest hex
    // (Centaur stops behind the last one), or the furthest with maxRange (Rogue). The target passed to EnemiesNear may
    // already be dead (Warlock passes the curse when the host dies).
    internal class FindTarget
    {
        private const float TieEpsilon = 0.01f;

        private readonly Hero _me;
        private readonly EnemyScan _scan;

        private ICombatant _lastNearest;
        private ICombatant _currentTarget;
        internal ICombatant CurrentTarget => _currentTarget;

        private ICombatant _chosenTarget;

        internal void ForgetScanCaches() => _scan.Invalidate();

        public FindTarget(Hero me, BattleBoard board)
        {
            _me = me;
            _scan = new EnemyScan(me, board);
        }

        public ICombatant FindCurrentTarget()
        {
            if (IsStillEngagedWith(_currentTarget)) return _currentTarget;

            ICombatant fresh = FindNearestEnemy();
            _currentTarget = fresh;

            return fresh;
        }

        public ICombatant FindNearestEnemy()
        {
            List<(ICombatant target, int steps)> enemySteps = Candidates();

            if (enemySteps.Count == 0) return null;

            if (IsChosenTargetOpen(enemySteps))
            {
                _lastNearest = _chosenTarget;
                return _chosenTarget;
            }

            int nearestSteps = enemySteps.Min(e => e.steps);
            var tiedNearest = enemySteps.Where(e => e.steps == nearestSteps).Select(e => e.target).ToList();

            ICombatant preferred = _lastNearest ?? _currentTarget;

            ICombatant nearestEnemy = BreakTie(tiedNearest, preferred);
            _lastNearest = nearestEnemy;

            return nearestEnemy;
        }

        internal List<ICombatant> TiedNearestEnemies()
        {
            List<(ICombatant target, int steps)> enemySteps = Candidates();
            if (enemySteps.Count == 0) return new List<ICombatant>();

            int nearestSteps = enemySteps.Min(e => e.steps);
            return enemySteps
                .Where(e => e.steps == nearestSteps && e.target.CurrentHex() != null)
                .Select(e => e.target)
                .OrderBy(target => target.CurrentHex().transform.position.x)
                .ThenByDescending(target => target.CurrentHex().transform.position.y)
                .ToList();
        }

        internal ICombatant ShownChosenTarget()
            => _chosenTarget != null && TiedNearestEnemies().Contains(_chosenTarget) ? _chosenTarget : null;

        internal bool SwitchChosenTarget()
        {
            List<ICombatant> tied = TiedNearestEnemies();
            if (tied.Count < 2) return false;

            int at = tied.IndexOf(ShownChosenTarget());
            _chosenTarget = at + 1 < tied.Count ? tied[at + 1] : null;
            return true;
        }

        internal ICombatant TargetInFight
        {
            get
            {
                if (IsStillEngagedWith(_currentTarget)) return _currentTarget;

                return _lastNearest != null && _lastNearest.IsAlive ? _lastNearest : null;
            }
        }

        internal void KeepChosenTargetOnlyIfTied() => _chosenTarget = ShownChosenTarget();

        public ICombatant FindFurthestEnemy(int reachRange) => FindFurthestEnemies(reachRange).FirstOrDefault();

        public IReadOnlyList<ICombatant> FindFurthestEnemies(int reachRange, int count = 1)
        {
            var inReach = _scan.Distances().Where(e => e.dist <= reachRange).Select(e => e.target).ToList();
            if (inReach.Count == 0) return Array.Empty<ICombatant>();

            Dictionary<Hex, int> hexesAway = HexesAwayFromMe();
            int HexesTo(ICombatant enemy)
                => enemy.CurrentHex() is Hex hex && hexesAway.TryGetValue(hex, out int n) ? n : 0;

            return inReach
                .Select(enemy => (enemy, hexes: HexesTo(enemy), shuffle: UnityEngine.Random.value))
                .OrderByDescending(e => e.hexes)
                .ThenBy(e => e.shuffle)
                .Select(e => e.enemy)
                .Take(count)
                .ToList();
        }

        private Dictionary<Hex, int> HexesAwayFromMe()
        {
            var hexesAway = new Dictionary<Hex, int>();
            Hex myHex = _me.CurrentHex;
            if (myHex == null) return hexesAway;

            hexesAway[myHex] = 0;
            var frontier = new Queue<Hex>();
            frontier.Enqueue(myHex);
            while (frontier.Count > 0)
            {
                Hex hex = frontier.Dequeue();
                foreach (Hex neighbor in hex.GetNeighbors())
                {
                    if (hexesAway.ContainsKey(neighbor)) continue;
                    hexesAway[neighbor] = hexesAway[hex] + 1;
                    frontier.Enqueue(neighbor);
                }
            }
            return hexesAway;
        }

        public IPlacement FindClusteredCircle(int reachRange, float blastRadius, bool isJump)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<Hex> candidates = null;

            if (isJump)
            {
                candidates = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther);
            }

            else
            {
                candidates = HexFinder.FindHexesWithin(myHex, reachRange);
            }

            if (candidates == null || candidates.Count == 0) return null;

            return ClusterAim.BestCircle(myHex.transform.position, candidates, enemies, blastRadius);
        }

        public IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth, bool maxRange = false)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<Hex> landings = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther);

            return ClusterAim.BestCharge(myHex.transform.position, landings, enemies, chargeHalfWidth, maxRange);
        }

        public IPlacement FindRandomFreeHex(int reachRange)
        {
            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<Hex> landings = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther)
                .Where(hex => hex != myHex)
                .ToList();

            if (landings.Count == 0) return null;

            return landings[UnityEngine.Random.Range(0, landings.Count)];
        }

        public ICombatant FindClusteredLaser(int reachRange, float beamHalfWidth)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            List<ICombatant> bestAims = ClusterAim.BestLaserAims(myHex.transform.position, enemies, reachRange, beamHalfWidth, out int caught);

            bool noOtherEnemyBesideTarget = caught <= 0;
            if (noOtherEnemyBesideTarget) return UseCurrentTargetOrNewTarget();

            return BreakTie(bestAims, _currentTarget);
        }

        public IReadOnlyList<ICombatant> FindEnemiesNear(ICombatant target, int reachRange)
        {
            Hex targetHex = target?.CurrentHex();
            if (targetHex == null) return Array.Empty<ICombatant>();

            HashSet<Hex> nearby = new HashSet<Hex>(HexFinder.FindHexesWithin(targetHex, reachRange));

            return _scan.GetAllEnemy().Where(enemy => enemy.CurrentHex() != null && nearby.Contains(enemy.CurrentHex())).ToList();
        }

        public ICombatant FindNearestEnemyTo(ICombatant target)
        {
            Hex targetHex = target?.CurrentHex();
            if (targetHex == null) return null;

            return _scan.GetAllEnemy()
                .Where(enemy => !ReferenceEquals(enemy, target) && enemy.IsAlive && enemy.CurrentHex() != null)
                .OrderBy(enemy => Vector3.Distance(targetHex.transform.position, enemy.CurrentHex().transform.position))
                .FirstOrDefault();
        }

        public ICombatant FindNearestAlly()
        {
            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            return _scan.GetAllAlly()
                .Where(ally => ally.CurrentHex() != null)
                .OrderBy(ally => Vector3.Distance(myHex.transform.position, ally.CurrentHex().transform.position))
                .FirstOrDefault();
        }

        public ICombatant FindLowestHealthAlly()
        {
            return _scan.GetAllAlly()
                .Where(ally => ally.CurrentHex() != null && ally is IHeroStats stats && stats.MaxHP > 0)
                .OrderBy(ally => (float)((IHeroStats)ally).CurrentHP / ((IHeroStats)ally).MaxHP)
                .FirstOrDefault();
        }

        public IReadOnlyList<ICombatant> FindRandomEnemies(int count)
            => _scan.GetAllEnemy().OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();

        public IReadOnlyList<ICombatant> FindAllEnemies() => _scan.GetAllEnemy();
        public IReadOnlyList<ICombatant> FindAllAllies() => _scan.GetAllAlly();

        private ICombatant BreakTie(List<ICombatant> tied, ICombatant preferred)
        {
            if (tied.Count == 1) return tied[0];

            if (preferred != null && preferred.IsAlive && tied.Contains(preferred)) return preferred;

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

            return tied[UnityEngine.Random.Range(0, tied.Count)];
        }

        private List<(ICombatant target, int steps)> Candidates()
        {
            List<(ICombatant target, int steps)> enemySteps = _scan.Steps();

            var inRange = enemySteps.Where(e => IsInMyRange(e.target)).ToList();
            return inRange.Count > 0 ? inRange : enemySteps;
        }

        private bool IsChosenTargetOpen(List<(ICombatant target, int steps)> candidates)
        {
            if (_chosenTarget == null || !_chosenTarget.IsAlive) return false;

            return candidates.Any(e => e.target == _chosenTarget && e.steps < EnemyScan.UnreachablePenalty);
        }

        private bool IsInMyRange(ICombatant target)
        {
            Hex myHex = _me.CurrentHex;
            Hex targetHex = target.CurrentHex();
            return myHex != null && targetHex != null && myHex.IsWithinRange(targetHex, _me.Range);
        }

        private ICombatant UseCurrentTargetOrNewTarget()
        {
            ICombatant engaged = _currentTarget;

            if (engaged != null && engaged.IsAlive && engaged.IsInCombat) return engaged;

            return FindNearestEnemy();
        }

        private bool IsStillEngagedWith(ICombatant engaged)
        {
            if (engaged == null || !engaged.IsAlive || !engaged.IsInCombat) return false;

            if (engaged.HasStatus(ModifierEnum.Untargetable)) return false;

            Hex myHex = _me.CurrentHex;
            if (myHex == null || engaged.CurrentHex() == null) return false;

            return myHex.IsWithinRange(engaged.CurrentHex(), _me.Range);
        }
    }
}
