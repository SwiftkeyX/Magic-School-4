using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    internal class EnemyScan
    {
        // Ranks an enemy nothing can currently reach behind every enemy something can, while
        // still ordering those by the walk they would take if the way cleared. Bigger than any
        // step count a board this size can produce.
        internal const int UnreachablePenalty = 1000;

        private readonly Hero _me;
        private readonly BattleBoard _board;


        private Dictionary<Hex, int> _stepsFromMe;          // steps from me, routing around whoever is in the way
        private Dictionary<Hex, int> _stepsIfBoardWereEmpty; // steps from me, ignoring everyone
        private int _stepMapFrame = -1;

        // =================================== cache ===================================
        private List<ICombatant> _enemyCache;
        private List<(ICombatant target, float dist)> _distanceCache;
        private List<(ICombatant target, int steps)> _stepCache;
        private int _enemyCacheFrame = -1;
        private int _distanceCacheFrame = -1;
        private int _stepCacheFrame = -1;


        public EnemyScan(Hero me, BattleBoard board)
        {
            _me = me;
            _board = board;
        }

        // ========================================= public =========================================
        // Get every enemy on the board.
        public List<ICombatant> GetAllEnemy()
        {
            // a cache from earlier this frame drops whoever died since: a hero killed this frame is not an enemy to go for
            // e.g.     Skeleton Archer walking toward the slime his own arrow just killed
            bool isCache = _enemyCacheFrame == Time.frameCount;
            if (isCache) return _enemyCache = WithoutTheDead(_enemyCache, enemy => enemy);

            _enemyCache = _board.HeroesOnBoard.Where(IsEnemy).ToList();
            _enemyCacheFrame = Time.frameCount;

            return _enemyCache;
        }

        // Get every ally on the board.
        public List<ICombatant> GetAllAlly()
        {
            return _board.HeroesOnBoard.Where(IsAlly).ToList();
        }

        // What is the "distance" between me and each target.
        // distance = the distance in straight line.
        public List<(ICombatant target, float dist)> Distances()
        {
            bool isCache = _distanceCacheFrame == Time.frameCount;
            if (isCache) return _distanceCache = WithoutTheDead(_distanceCache, e => e.target);

            Vector3 origin = _me.CurrentHex.transform.position;

            _distanceCache = GetAllEnemy()
                .Select(target => (target, dist: Vector3.Distance(origin, target.CurrentHex().transform.position)))
                .ToList();
            _distanceCacheFrame = Time.frameCount;

            return _distanceCache;
        }

        // How many "steps" to walk to each target, that I CAN actually get to.
        // steps = the least amount of hop until the target is in my range: for a melee hero, until I stand next to it;
        // for a ranged one, until I could shoot it - 0 when I already can.
        // e.g.     Ranger, range 4, two enemies 5 and 6 hexes away => 1 step each: a tie, though one is a hex nearer
        public List<(ICombatant target, int steps)> Steps()
        {
            bool isCache = _stepCacheFrame == Time.frameCount;
            if (isCache) return _stepCache = WithoutTheDead(_stepCache, e => e.target);

            RefreshStepMaps();

            _stepCache = GetAllEnemy().Select(target => (target, steps: StepsToReach(target))).ToList();
            _stepCacheFrame = Time.frameCount;

            return _stepCache;
        }

        // ========================================= private =========================================
        // a new list, not the cached one edited: whoever is still looping over the old one is left alone
        private static List<T> WithoutTheDead<T>(List<T> cache, System.Func<T, ICombatant> unit)
            => cache.TrueForAll(entry => unit(entry).IsAlive) ? cache : cache.Where(entry => unit(entry).IsAlive).ToList();

        // easy boolean logic to filter the enemy
        private bool IsEnemy(ICombatant target)
        {
            bool notTargetMyself = target != _me as ICombatant;
            bool notTargetFriend = target.Team != _me.Team;
            bool notTargetDead = target.IsAlive;
            bool notTargetGuyNotInCombat = target.IsInCombat;
            bool notTargetGuyWhoVanished = !target.HasStatus(ModifierEnum.Untargetable);
            return notTargetMyself && notTargetFriend && notTargetDead && notTargetGuyNotInCombat && notTargetGuyWhoVanished;
        }

        // easy boolean logic to filter the ally
        private bool IsAlly(ICombatant target)
        {
            bool notMyself = target != _me as ICombatant;
            bool sameTeam = target.Team == _me.Team;
            return notMyself && sameTeam && target.IsAlive && target.IsInCombat;
        }

        // get number of hop from me to specify enemy
        // FLAGGING: what is _stepsIfBoardWereEmpty here for?
        private int StepsToReach(ICombatant enemy)
        {
            // measured to where it is arriving: an enemy mid-step is already drawn there, and stands there in a moment
            Hex enemyHex = enemy.ArrivingHex();
            if (enemyHex == null) return UnreachablePenalty;

            int range = Mathf.Max(1, _me.Range);
            int walked = int.MaxValue;
            int ifTheWayCleared = int.MaxValue;

            // every hex I could hit it from: within my range of it, by hex distance, the way Hex.IsWithinRange counts it
            foreach (KeyValuePair<Hex, int> fromEnemy in HexFinder.StepsFrom(enemyHex, null))
            {
                if (fromEnemy.Value == 0 || fromEnemy.Value > range) continue;

                Hex hitFrom = fromEnemy.Key;
                if (_stepsFromMe.TryGetValue(hitFrom, out int steps)) walked = Mathf.Min(walked, steps);
                if (_stepsIfBoardWereEmpty.TryGetValue(hitFrom, out int open)) ifTheWayCleared = Mathf.Min(ifTheWayCleared, open);
            }

            if (walked != int.MaxValue) return walked;

            // boxed in by whoever is standing around it, for now
            return UnreachablePenalty + (ifTheWayCleared == int.MaxValue ? 0 : ifTheWayCleared);
        }

        // Both maps are wanted together and cost the same walk of the board, so they are built
        // in one go.
        private void RefreshStepMaps()
        {
            bool isCache = _stepMapFrame == Time.frameCount;
            if (isCache) return;

            _stepsFromMe = HexFinder.StepsFrom(_me.CurrentHex, _me.IsHexReservedByOther);
            _stepsIfBoardWereEmpty = HexFinder.StepsFrom(_me.CurrentHex, null);
            _stepMapFrame = Time.frameCount;
        }
    }
}
