using System;
using System.Collections;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// A template action whose only job is to repeat another template action several times.
    /// e.g.    Skeleton Archer's 4 sequential shots
    ///         a volley of 8 arrows at once 
    /// The runner, not its shots, is what expires: a flow waiting on OnExpired (the next part, or the next round of a
    /// Repeat) is told once, after the last shot is fired and PauseAfter has passed - never once per shot.
    // Has no source or aim of its own: it remembers what it was told, so each shot resolves its own. It raises
    // OnExpired itself and hands every other callback to each shot. A caster whose object is gone (cleared off a
    // benchmark board, or sold) stops it; one that only died keeps firing.
    internal abstract class FireTimingRunnerBase<TTuning> : TemplateAction<TTuning>
        where TTuning : FireTimingRunnerTuning
    {
        [SerializeField] protected TemplateAction _innerPrefab;

        protected ActionSourceEnum _innerSource;
        protected AimTargetEnum _innerAimTarget;

        private bool _warnedAboutRandom;

        protected Tuning _innerTuning;
        protected int _count = 1;
        private FireTimingModeEnum _mode = FireTimingModeEnum.AtOnce;
        private float _interval;
        private float _pauseAfter;

        protected override void SubscribeTriggers(TemplateActionCallbacks callbacks) => base.SubscribeTriggers(callbacks);

        private TemplateActionCallbacks _shotCallbacks;
        protected TemplateActionCallbacks ShotCallbacks
            => _shotCallbacks ??= new TemplateActionCallbacks { OnHit = _callbacks.OnHit, OnSkillHit = _callbacks.OnSkillHit };

        protected override void ApplyTypedTuning(TTuning tuning)
        {
            if (tuning.Count.HasValue) _count = tuning.Count.Value;
            if (tuning.Mode.HasValue) _mode = tuning.Mode.Value;
            if (tuning.Interval.HasValue) _interval = tuning.Interval.Value;
            if (tuning.PauseAfter.HasValue) _pauseAfter = tuning.PauseAfter.Value;
            if (tuning.InnerTuning != null) _innerTuning = tuning.InnerTuning;
        }

        protected override void Play()
        {
            if (_mode == FireTimingModeEnum.AtOnce)
            {
                for (int i = 0; i < _count; i++) FireOnce();
                Finish();
            }

            else if (_mode == FireTimingModeEnum.Sequence)
            {
                StartCoroutine(FireSequence());
            }
        }

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

        protected virtual void FireOnce()
        {
            AimTargetEnum shotAimTarget = _innerAimTarget;

            SkillPart innerGroup = new SkillPart(
                source: _innerSource,
                templateAction: _innerPrefab,
                target: shotAimTarget,
                effects: _effects,
                tuning: _innerTuning
            );

            TemplateAction.TryPlay(innerGroup, _me, ShotCallbacks, _previousPosition, _assignedTarget);
        }

        private IEnumerator FireSequence()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_me is UnityEngine.Object caster && caster == null)
                {
                    DestroyMe();
                    yield break;
                }

                FireOnce();

                bool isLastShot = (i == _count - 1);
                if (!isLastShot) yield return new WaitForSeconds(_interval);
            }

            Finish();
        }

        private void Finish()
        {
            if (_pauseAfter > 0f) ExpireAfter(_pauseAfter);
            else DestroyMe();
        }
    }
}
