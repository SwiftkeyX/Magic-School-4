using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// A runner whose inner action is a Projectile.
    /// It's the only one that can aim AimTargetEnum.Random, which mean it pick the the random target for the Projectile. 
    internal class FireTimingRunnerProjectile : FireTimingRunnerBase<FireTimingRunnerProjectileTuning>
    {
        // ======================================= tune able =======================================
        private int _randomPoolRadius;
        private RandomTargetPool _randomTargets;

        protected override void ApplyTypedTuning(FireTimingRunnerProjectileTuning tuning)
        {
            base.ApplyTypedTuning(tuning);

            if (tuning.RandomPoolRadius.HasValue) _randomPoolRadius = tuning.RandomPoolRadius.Value;
        }

        // ======================================= override =======================================
        protected override void FireOnce()
        {
            AimTargetEnum shotAimTarget = _innerAimTarget;
            SkillStepContext shotContext = _fromPreviousStep;
            
            // if aim = random, the runner have to be the organizer for picking the random unit.
            if (_innerAimTarget == AimTargetEnum.Random)
            {
                ICombatant chosen = PickRandomTarget();
                if (chosen == null) return;

                shotAimTarget = AimTargetEnum.Assigned;
                shotContext = new SkillStepContext(chosen);
            }

            // copy/paste to create a desired template action
            SkillActionGroup innerGroup = new SkillActionGroup(
                source: _innerSource,
                templateAction: _innerPrefab,
                target: shotAimTarget,
                effects: _effects,
                tuning: _innerTuning
            );

            TemplateAction.TryPlay(innerGroup, _me, _onExpired, _onHit, shotContext);
        }

        // ======================================= private =======================================
        // pick random target using Sparks logic
        private ICombatant PickRandomTarget()
        {
            if (_randomTargets == null) _randomTargets = new RandomTargetPool(_me, _randomPoolRadius);

            return _randomTargets.Next();
        }
    }
}
