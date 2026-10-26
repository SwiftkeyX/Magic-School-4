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
    // Furthest is picked on a barrage's first shot and kept until that enemy dies, since a tie for furthest is random
    // and shots left to aim themselves could split (Skeleton Archer's shots all go to the one Minotaur). FurthestEach
    // picks a different one per shot (Goblin Slinger).
    internal class FireTimingRunnerProjectile : FireTimingRunnerBase<FireTimingRunnerProjectileTuning>
    {
        private int _randomPoolRadius;
        private RandomTargetPool _randomTargets;

        private IReadOnlyList<ICombatant> _furthestTargets;
        private int _nextFurthest;

        private ICombatant _barrageTarget;

        protected override void ApplyTypedTuning(FireTimingRunnerProjectileTuning tuning)
        {
            base.ApplyTypedTuning(tuning);

            if (tuning.RandomPoolRadius.HasValue) _randomPoolRadius = tuning.RandomPoolRadius.Value;
        }

        protected override void FireOnce()
        {
            AimTargetEnum shotAimTarget = _innerAimTarget;
            ICombatant shotTarget = _assignedTarget;

            if (_innerAimTarget == AimTargetEnum.Random)
            {
                ICombatant chosen = PickRandomTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            else if (_innerAimTarget == AimTargetEnum.Furthest)
            {
                ICombatant chosen = PickBarrageTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            else if (_innerAimTarget == AimTargetEnum.FurthestEach)
            {
                ICombatant chosen = PickNextFurthestTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotTarget = chosen;
            }

            SkillPart innerGroup = new SkillPart(
                source: _innerSource,
                templateAction: _innerPrefab,
                target: shotAimTarget,
                effects: _effects,
                tuning: _innerTuning
            );

            TemplateAction.TryPlay(innerGroup, _me, ShotCallbacks, _previousPosition, shotTarget);
        }

        private ICombatant PickRandomTarget()
        {
            if (_randomTargets == null) _randomTargets = new RandomTargetPool(_me, _randomPoolRadius);

            return _randomTargets.Next();
        }

        private ICombatant PickBarrageTarget()
        {
            if (_barrageTarget != null && _barrageTarget.IsAlive) return _barrageTarget;

            int reachRange = (_innerTuning as ProjectileTuning)?.Range ?? int.MaxValue;
            return _barrageTarget = _me.FindFurthestEnemy(reachRange);
        }

        private ICombatant PickNextFurthestTarget()
        {
            if (_furthestTargets == null)
            {
                int reachRange = (_innerTuning as ProjectileTuning)?.Range ?? int.MaxValue;
                _furthestTargets = _me.FindFurthestEnemies(reachRange, _count);
            }

            while (_nextFurthest < _furthestTargets.Count && !_furthestTargets[_nextFurthest].IsAlive) _nextFurthest++;

            if (_nextFurthest >= _furthestTargets.Count) return null;

            return _furthestTargets[_nextFurthest++];
        }
    }
}
