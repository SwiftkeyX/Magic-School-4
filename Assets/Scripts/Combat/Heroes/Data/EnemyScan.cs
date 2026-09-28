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
        private const int UnreachablePenalty = 1000;

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
            bool isCache = _enemyCacheFrame == Time.frameCount;
            if (isCache) return _enemyCache;

            _enemyCache = _board.HeroesOnBoard.Where(IsEnemy).ToList();
            _enemyCacheFrame = Time.frameCount;

            return _enemyCache;
        }

        // What is the "distance" between me and each target.
        // distance = the distance in straight line.
        public List<(ICombatant target, float dist)> Distances()
        {
            bool isCache = _distanceCacheFrame == Time.frameCount;
            if (isCache) return _distanceCache;

            Vector3 origin = _me.CurrentHex.transform.position;

            _distanceCache = GetAllEnemy()
                .Select(target => (target, dist: Vector3.Distance(origin, target.CurrentHex().transform.position)))
                .ToList();
            _distanceCacheFrame = Time.frameCount;

            return _distanceCache;
        }

        // How many "steps" to walk to each target, that I CAN actually get to. 
        // steps = the least amount of hop that me can get to target.
        public List<(ICombatant target, int steps)> Steps()
        {
            bool isCache = _stepCacheFrame == Time.frameCount;
            if (isCache) return _stepCache;

            RefreshStepMaps();

            _stepCache = GetAllEnemy().Select(target => (target, steps: StepsToReach(target))).ToList();
            _stepCacheFrame = Time.frameCount;

            return _stepCache;
        }

        // ========================================= private =========================================
        // easy boolean logic to filter the enemy
        private bool IsEnemy(ICombatant target)
        {
            bool notTargetMyself = target != _me as ICombatant;
            bool notTargetFriend = target.Team != _me.Team;
            bool notTargetDead = target.IsAlive;
            bool notTargetGuyNotInCombat = target.IsInCombat;
            return notTargetMyself && notTargetFriend && notTargetDead && notTargetGuyNotInCombat;
        }

        // get number of hop from me to specify enemy
        // FLAGGING: what is _stepsIfBoardWereEmpty here for?
        private int StepsToReach(ICombatant enemy)
        {
            Hex enemyHex = enemy.CurrentHex();
            if (enemyHex == null) return UnreachablePenalty;

            int walked = int.MaxValue;
            int ifTheWayCleared = int.MaxValue;

            foreach (Hex beside in enemyHex.GetNeighbors())
            {
                if (_stepsFromMe.TryGetValue(beside, out int steps)) walked = Mathf.Min(walked, steps);
                if (_stepsIfBoardWereEmpty.TryGetValue(beside, out int open)) ifTheWayCleared = Mathf.Min(ifTheWayCleared, open);
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
