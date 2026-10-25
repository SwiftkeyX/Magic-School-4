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
    internal class FindTarget
    {
        private const float TieEpsilon = 0.01f;     // How close two enemies' distances have to be to count as tied.

        private readonly Hero _me;
        private readonly EnemyScan _scan;

        private ICombatant _lastNearest;
        private ICombatant _currentTarget;
        internal ICombatant CurrentTarget => _currentTarget;

        // the enemy the player picked for me among the tied nearest, in Preparation. null = automatic (See SwitchChosenTarget)
        private ICombatant _chosenTarget;

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

        // Picks the enemy that costs the fewest steps to walk until it is in my range
        // consider step as hop amount (See EnemyScan.Steps)
        public ICombatant FindNearestEnemy()
        {
            List<(ICombatant target, int steps)> enemySteps = Candidates();

            if (enemySteps.Count == 0) return null;

            // the player's chosen target is sticky: walked to while it can be reached, unless another enemy is already in my range
            if (IsChosenTargetOpen(enemySteps))
            {
                _lastNearest = _chosenTarget;
                return _chosenTarget;
            }

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

        // ========================================= chosen target =========================================
        // When my nearest enemy is a tie, the player may pick which one I go for: right-clicking me again while I am inspected,
        // in Preparation, steps through the tied enemies and then back to automatic. Once the fight starts the pick is sticky.
        // e.g.     Orc and Ranger both pick the same enemy, so Troll's allies land the kill together

        // The enemies tied for nearest from where I stand now, in a fixed order (left first, then top) so switching steps through them.
        // The same tie FindNearestEnemy breaks: an enemy already in my range wins first.
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

        // My pick, while it is still one of the tied nearest; null = automatic
        internal ICombatant ShownChosenTarget()
            => _chosenTarget != null && TiedNearestEnemies().Contains(_chosenTarget) ? _chosenTarget : null;

        // Step to the next tied enemy, and after the last one back to automatic. False when there is no tie to choose in.
        internal bool SwitchChosenTarget()
        {
            List<ICombatant> tied = TiedNearestEnemies();
            if (tied.Count < 2) return false;

            int at = tied.IndexOf(ShownChosenTarget());     // -1 while automatic
            _chosenTarget = at + 1 < tied.Count ? tied[at + 1] : null;
            return true;
        }

        // Who I am going for in the fight right now: the enemy I am engaged with, else the one I last walked toward.
        // Read by the Shift view in a fight, as a debug tool (See AlignHighlight.ShowFightTargetView)
        internal ICombatant TargetInFight
        {
            get
            {
                if (IsStillEngagedWith(_currentTarget)) return _currentTarget;

                return _lastNearest != null && _lastNearest.IsAlive ? _lastNearest : null;
            }
        }

        // At the start of a fight a pick counts only if it is still tied: moving me afterwards cannot aim me at anyone,
        // and an enemy from the stage before is gone
        internal void KeepChosenTargetOnlyIfTied() => _chosenTarget = ShownChosenTarget();

        // Picks furthest enemy within reachRange
        public ICombatant FindFurthestEnemy(int reachRange) => FindFurthestEnemies(reachRange).FirstOrDefault();

        // Picks [count] furthest enemies within reachRange
        // furthest first, second furthest, third furthest, and so on ...
        // Furthest is counted in hexes, and enemies the same hexes away are shuffled: measured in world distance, a diagonal
        // hex (~1.118 apart) always beat a straight one (1.0), so of two enemies 5 hexes away the same one won every time.
        // e.g.     Goblin Slinger shoots the 2 furthest enemies
        //          Skeleton Archer, two enemies 6 hexes away, one above and one below => either, at random, every barrage
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

        // every hex of the board with how many hexes it is from mine, counted step by step (as Hex.IsWithinRange counts)
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
        // When several landings hit the same enemies count:
        //      the closest hex wins
        //      the furthest hex if maxRange
        // e.g.     
        //      Centaur stops right behind the last enemy he hit
        //      Rogue keeps charging as far as he can
        public IPlacement FindClusteredCharge(int reachRange, float chargeHalfWidth, bool maxRange = false)
        {
            List<ICombatant> enemies = _scan.GetAllEnemy();
            if (enemies.Count == 0) return null;

            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            // standing still is not a charge - there would be no path to sweep
            List<Hex> landings = HexFinder.FindFreeHexesWithin(myHex, reachRange, _me.IsHexReservedByOther);

            return ClusterAim.BestCharge(myHex.transform.position, landings, enemies, chargeHalfWidth, maxRange);
        }

        // Pick a random free hex within my reach range (not the one I stand on)
        // e.g. Rogue dashes 1 hex in a random direction
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

        // Find a enemy closest to target
        // NOTE: target may already be dead
        // e.g. Warlock passes the curse when the host dies
        public ICombatant FindNearestEnemyTo(ICombatant target)
        {
            Hex targetHex = target?.CurrentHex();
            if (targetHex == null) return null;

            return _scan.GetAllEnemy()
                .Where(enemy => !ReferenceEquals(enemy, target) && enemy.IsAlive && enemy.CurrentHex() != null)
                .OrderBy(enemy => Vector3.Distance(targetHex.transform.position, enemy.CurrentHex().transform.position))
                .FirstOrDefault();
        }

        // Find the ally closest to me 
        public ICombatant FindNearestAlly()
        {
            Hex myHex = _me.CurrentHex;
            if (myHex == null) return null;

            return _scan.GetAllAlly()
                .Where(ally => ally.CurrentHex() != null)
                .OrderBy(ally => Vector3.Distance(myHex.transform.position, ally.CurrentHex().transform.position))
                .FirstOrDefault();
        }

        // Find a ally with the smallest percentage HP left.
        public ICombatant FindLowestHealthAlly()
        {
            return _scan.GetAllAlly()
                .Where(ally => ally.CurrentHex() != null && ally is IHeroStats stats && stats.MaxHP > 0)
                .OrderBy(ally => (float)((IHeroStats)ally).CurrentHP / ((IHeroStats)ally).MaxHP)
                .FirstOrDefault();
        }

        // Find random x enemies.
        public IReadOnlyList<ICombatant> FindRandomEnemies(int count)
            => _scan.GetAllEnemy().OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();

        // Find all allys or enemies.
        public IReadOnlyList<ICombatant> FindAllEnemies() => _scan.GetAllEnemy();
        public IReadOnlyList<ICombatant> FindAllAllies() => _scan.GetAllAlly();

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

        // every enemy I can pick from, with its steps: an enemy already in my range is picked before any other
        private List<(ICombatant target, int steps)> Candidates()
        {
            List<(ICombatant target, int steps)> enemySteps = _scan.Steps();

            var inRange = enemySteps.Where(e => IsInMyRange(e.target)).ToList();
            return inRange.Count > 0 ? inRange : enemySteps;
        }

        // my chosen target is among the candidates and can be walked to - not boxed in
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

            // Is current target vanished?
            if (engaged.HasStatus(ModifierEnum.Untargetable)) return false;

            Hex myHex = _me.CurrentHex;
            if (myHex == null || engaged.CurrentHex() == null) return false;

            // Is current target is out of range?
            return myHex.IsWithinRange(engaged.CurrentHex(), _me.Range);
        }
    }
}
