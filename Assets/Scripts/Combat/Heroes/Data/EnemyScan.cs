using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Heroes
{
    // Steps = the fewest hops until the target is in range: next to it for melee, within range for ranged (0 when it
    // already can). A Ranger with range 4 and enemies 5 and 6 hexes away is 1 step from each: a tie. An enemy mid-step
    // is measured to where it is arriving. An enemy nothing can reach right now ranks behind every reachable one
    // (UnreachablePenalty), still ordered by the walk it would take if the way cleared.
    // The caches are per frame and drop whoever died since they were built, into a new list (whoever loops the old one
    // is left alone): Skeleton Archer once walked toward the slime his own arrow had just killed. Both step maps cost
    // the same walk of the board, so they are built together. Invalidate drops every cache for EditMode tests, where
    // no frame passes.
    internal class EnemyScan
    {
        internal const int UnreachablePenalty = 1000;

        private readonly Hero _me;
        private readonly BattleBoard _board;


        private Dictionary<Hex, int> _stepsFromMe;
        private Dictionary<Hex, int> _stepsIfBoardWereEmpty;
        private int _stepMapFrame = -1;

        private List<ICombatant> _enemyCache;
        private List<(ICombatant target, float dist)> _distanceCache;
        private List<(ICombatant target, int steps)> _stepCache;
        private int _enemyCacheFrame = -1;
        private int _distanceCacheFrame = -1;
        private int _stepCacheFrame = -1;

        internal void Invalidate()
        {
            _stepMapFrame = -1;
            _enemyCacheFrame = -1;
            _distanceCacheFrame = -1;
            _stepCacheFrame = -1;
        }

        public EnemyScan(Hero me, BattleBoard board)
        {
            _me = me;
            _board = board;
        }

        public List<ICombatant> GetAllEnemy()
        {
            bool isCache = _enemyCacheFrame == Time.frameCount;
            if (isCache) return _enemyCache = WithoutTheDead(_enemyCache, enemy => enemy);

            _enemyCache = _board.HeroesOnBoard.Where(IsEnemy).ToList();
            _enemyCacheFrame = Time.frameCount;

            return _enemyCache;
        }

        public List<ICombatant> GetAllAlly()
        {
            return _board.HeroesOnBoard.Where(IsAlly).ToList();
        }

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

        public List<(ICombatant target, int steps)> Steps()
        {
            bool isCache = _stepCacheFrame == Time.frameCount;
            if (isCache) return _stepCache = WithoutTheDead(_stepCache, e => e.target);

            RefreshStepMaps();

            _stepCache = GetAllEnemy().Select(target => (target, steps: StepsToReach(target))).ToList();
            _stepCacheFrame = Time.frameCount;

            return _stepCache;
        }

        private static List<T> WithoutTheDead<T>(List<T> cache, System.Func<T, ICombatant> unit)
            => cache.TrueForAll(entry => unit(entry).IsAlive) ? cache : cache.Where(entry => unit(entry).IsAlive).ToList();

        private bool IsEnemy(ICombatant target)
        {
            bool notTargetMyself = target != _me as ICombatant;
            bool notTargetFriend = target.Team != _me.Team;
            bool notTargetDead = target.IsAlive;
            bool notTargetGuyNotInCombat = target.IsInCombat;
            bool notTargetGuyWhoVanished = !target.HasStatus(ModifierEnum.Untargetable);
            return notTargetMyself && notTargetFriend && notTargetDead && notTargetGuyNotInCombat && notTargetGuyWhoVanished;
        }

        private bool IsAlly(ICombatant target)
        {
            bool notMyself = target != _me as ICombatant;
            bool sameTeam = target.Team == _me.Team;
            return notMyself && sameTeam && target.IsAlive && target.IsInCombat;
        }

        private int StepsToReach(ICombatant enemy)
        {
            Hex enemyHex = enemy.ArrivingHex();
            if (enemyHex == null) return UnreachablePenalty;

            int range = Mathf.Max(1, _me.Range);
            int walked = int.MaxValue;
            int ifTheWayCleared = int.MaxValue;

            foreach (KeyValuePair<Hex, int> fromEnemy in HexFinder.StepsFrom(enemyHex, null))
            {
                if (fromEnemy.Value == 0 || fromEnemy.Value > range) continue;

                Hex hitFrom = fromEnemy.Key;
                if (_stepsFromMe.TryGetValue(hitFrom, out int steps)) walked = Mathf.Min(walked, steps);
                if (_stepsIfBoardWereEmpty.TryGetValue(hitFrom, out int open)) ifTheWayCleared = Mathf.Min(ifTheWayCleared, open);
            }

            if (walked != int.MaxValue) return walked;

            return UnreachablePenalty + (ifTheWayCleared == int.MaxValue ? 0 : ifTheWayCleared);
        }

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
