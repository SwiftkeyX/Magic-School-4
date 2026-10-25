using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// A runner that repeating the projectile.
    /// e.g.    Skeleton shoot 4 projectile in sequence at furthest enemy
    /// 
    /// Additional target logic:
    ///         Random              Pick the random enemy inside the specify radius from the target point
    ///         FurthestEach        Pick [count] furthest enemy, different each time
    internal class FireTimingRunnerProjectile : FireTimingRunnerBase<FireTimingRunnerProjectileTuning>
    {
        // ======================================= tune able =======================================
        private int _randomPoolRadius;
        private RandomTargetPool _randomTargets;

        private IReadOnlyList<ICombatant> _furthestTargets;
        private int _nextFurthest;

        private ICombatant _barrageTarget;      // aim = Furthest: every shot of this barrage goes to it

        protected override void ApplyTypedTuning(FireTimingRunnerProjectileTuning tuning)
        {
            base.ApplyTypedTuning(tuning);

            if (tuning.RandomPoolRadius.HasValue) _randomPoolRadius = tuning.RandomPoolRadius.Value;
        }

        // ======================================= override =======================================
        protected override void FireOnce()
        {
            AimTargetEnum shotAimTarget = _innerAimTarget;
            ICombatant shotTarget = _assignedTarget;

            // if aim = random, the runner pick the random enemy itself.
            if (_innerAimTarget == AimTargetEnum.Random)
            {
                ICombatant chosen = PickRandomTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            // if aim = furthest, the runner picks it once and every shot goes there - a tie is random now, so a shot
            // left to aim itself could go elsewhere (See FindTarget.FindFurthestEnemies)
            else if (_innerAimTarget == AimTargetEnum.Furthest)
            {
                ICombatant chosen = PickBarrageTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            // if aim = furthest each, the runner pick the furthest enemy itself.
            else if (_innerAimTarget == AimTargetEnum.FurthestEach)
            {
                ICombatant chosen = PickNextFurthestTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            // copy/paste to create a desired template action
            SkillPart innerGroup = new SkillPart(
                source: _innerSource,
                templateAction: _innerPrefab,
                target: shotAimTarget,
                effects: _effects,
                tuning: _innerTuning
            );

            TemplateAction.TryPlay(innerGroup, _me, ShotCallbacks, _previousPosition, shotTarget);
        }

        // ======================================= private =======================================
        // pick random target inside the specify radius
        // e.g.     Imp shoot the projectile to random enemy inside 2 radius
        private ICombatant PickRandomTarget()
        {
            if (_randomTargets == null) _randomTargets = new RandomTargetPool(_me, _randomPoolRadius);

            return _randomTargets.Next();
        }

        // the furthest enemy, picked on the first shot of the barrage and kept; picked again only once it is dead
        // e.g.     Skeleton Archer's 3 shots all go to the one Minotaur, not split between two tied for furthest
        private ICombatant PickBarrageTarget()
        {
            if (_barrageTarget != null && _barrageTarget.IsAlive) return _barrageTarget;

            int reachRange = (_innerTuning as ProjectileTuning)?.Range ?? int.MaxValue;
            return _barrageTarget = _me.FindFurthestEnemy(reachRange);
        }

        // pick several furthest enemy but different one each time
        // e.g.     Goblin Slinger shoots the 2 furthest enemies
        private ICombatant PickNextFurthestTarget()
        {
            // find [count] furthest target
            if (_furthestTargets == null)
            {
                int reachRange = (_innerTuning as ProjectileTuning)?.Range ?? int.MaxValue;
                _furthestTargets = _me.FindFurthestEnemies(reachRange, _count);
            }

            // if target died mid logic, select the next furthest one 
            while (_nextFurthest < _furthestTargets.Count && !_furthestTargets[_nextFurthest].IsAlive) _nextFurthest++;

            if (_nextFurthest >= _furthestTargets.Count) return null;

            return _furthestTargets[_nextFurthest++];
        }
    }
}
