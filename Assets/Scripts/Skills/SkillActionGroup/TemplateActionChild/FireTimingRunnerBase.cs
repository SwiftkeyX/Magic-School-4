using System;
using System.Collections;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// A template action whose only job is to repeat another template action several times.
    /// e.g. Skeleton Archer's 4 sequential shots, or a volley of 8 arrows at once => those are projectile being repeated several time
    internal abstract class FireTimingRunnerBase<TTuning> : TemplateAction<TTuning>
        where TTuning : FireTimingRunnerTuning
    {
        [SerializeField] protected TemplateAction _innerPrefab;

        protected ActionSourceEnum _innerSource;
        protected AimTargetEnum _innerAimTarget;

        private bool _warnedAboutRandom;

        // ======================================= tune able =======================================
        protected Tuning _innerTuning;
        private FireTimingModeEnum _mode = FireTimingModeEnum.AtOnce;
        private int _count = 1;
        private float _interval;

        // ======================================= Event =======================================
        // The runner raises nothing itself - its callbacks are handed on to every shot it fires.
        protected override void SubscribeTriggers(TemplateActionCallbacks callbacks) { }


        // ======================================= override =======================================
        protected override void ApplyTypedTuning(TTuning tuning)
        {
            if (tuning.Count.HasValue) _count = tuning.Count.Value;
            if (tuning.Mode.HasValue) _mode = tuning.Mode.Value;
            if (tuning.Interval.HasValue) _interval = tuning.Interval.Value;
            if (tuning.InnerTuning != null) _innerTuning = tuning.InnerTuning;
        }

        protected override void Play()
        {
            // play once
            if (_mode == FireTimingModeEnum.AtOnce)
            {
                for (int i = 0; i < _count; i++) FireOnce();
                DestroyMe();
            }

            // repeat [count] time
            else if (_mode == FireTimingModeEnum.Sequence)
            {
                StartCoroutine(FireSequence());
            }
        }

        // FireTimingRunner has no source/aim of its own - it only remembers what it was told,
        // so each inner shot can resolve the real thing independently.
        protected override bool ResolveSource(ActionSourceEnum source)
        {
            _innerSource = source;
            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            _innerAimTarget = aimTarget;
            return true;
        }

        protected override Vector3 GetSpawnPosition() => _me.transform.position;

        // ======================================= main function =======================================
        protected virtual void FireOnce()
        {
            AimTargetEnum shotAimTarget = _innerAimTarget;

            // copy/paste to create a desired template action
            SkillPart innerGroup = new SkillPart(
                source: _innerSource,
                templateAction: _innerPrefab,
                target: shotAimTarget,
                effects: _effects,
                tuning: _innerTuning
            );

            TemplateAction.TryPlay(innerGroup, _me, _callbacks, _previousPosition, _assignedTarget);
        }

        private IEnumerator FireSequence()
        {
            for (int i = 0; i < _count; i++)
            {
                FireOnce();

                bool isLastShot = (i == _count - 1);
                if (!isLastShot) yield return new WaitForSeconds(_interval);
            }

            // after fire everything, destroy itself
            DestroyMe();
        }
    }
}
