using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;

namespace MagicSchool.Skills
{
    // The instance is only a driver, parked on the caster. The landing hex is reserved before the jump, so nobody
    // takes it. Landing ends the action, not a timer. While it moves its caster, IsMoving holds that caster's Cast
    // state: leaving Cast mid-air would start a step that carries it back to where it jumped from once it lands
    // (Knight's leap outlasts his cast time). No landing spot means no cast, rather than burning it on moving nowhere.
    // A caster destroyed mid-move (a benchmark lane cleared) is caught through `as Object`; the move then ends without
    // DestroyMe, so no landing part plays for a caster that no longer exists.
    internal class Move : TemplateAction<MoveTuning>
    {
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private int _jumpRange = 2;
        private float _jumpDuration = 0.5f;
        private float _spread = 1f;
        private bool _maxRange;
        private IPlacement _landing;
        private CurveMotion _jump;
        private bool _isJumping;

        private static readonly Dictionary<ICombatant, int> Moving = new Dictionary<ICombatant, int>();
        private bool _isCounted;

        internal static bool IsMoving(ICombatant caster) => caster != null && Moving.TryGetValue(caster, out int n) && n > 0;

        protected override void ApplyTypedTuning(MoveTuning moveTuning)
        {
            if (moveTuning.Range.HasValue) _jumpRange = moveTuning.Range.Value;
            if (moveTuning.Duration.HasValue) _jumpDuration = moveTuning.Duration.Value;
            if (moveTuning.Spread.HasValue) _spread = moveTuning.Spread.Value;
            if (moveTuning.MaxRange.HasValue) _maxRange = moveTuning.MaxRange.Value;
        }

        protected override void Play()
        {
            transform.position = GetSpawnPosition();

            _landing.OnUnitReserved(_me);

            _jump = new CurveMotion(
                start: _source,
                end: _landing.transform.position,
                duration: _jumpDuration,
                curve: _jumpCurve);
            _isJumping = true;
            Moving[_me] = (Moving.TryGetValue(_me, out int n) ? n : 0) + 1;
            _isCounted = true;

            SetLifeTime();
        }

        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
            }

            else
            {
                _source = _me.transform.position;
                return false;
            }

            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            if (aimTarget == AimTargetEnum.ClusteredCircle)
            {
                _landing = _me.FindClusteredCircle(_jumpRange, _spread, isJump: true);
                if (_landing == null) return false;

                _aimTarget = _landing.transform.position;
                return true;
            }

            else if (aimTarget == AimTargetEnum.ClusteredLaser)
            {
                _landing = _me.FindClusteredCharge(_jumpRange, _spread, _maxRange);
                if (_landing == null) return false;

                _aimTarget = _landing.transform.position;
                return true;
            }

            else if (aimTarget == AimTargetEnum.Random)
            {
                _landing = _me.FindRandomFreeHex(_jumpRange);
                if (_landing == null) return false;

                _aimTarget = _landing.transform.position;
                return true;
            }

            Debug.LogWarning($"[Scriptable Object] Move can't aim at {aimTarget}", this);
            return false;
        }

        protected override Vector3 GetSpawnPosition() => _source;

        protected override void SetLifeTime()
        {
            _lifetime = _jumpDuration;
        }

        private void OnDestroy() => StopCounting();

        private void StopCounting()
        {
            if (!_isCounted) return;
            _isCounted = false;

            if (Moving.TryGetValue(_me, out int n) && n > 1) Moving[_me] = n - 1;
            else Moving.Remove(_me);
        }

        private void Update()
        {
            if (!_isJumping) return;

            if (_me is Object caster && caster == null)
            {
                _isJumping = false;
                Destroy(gameObject);
                return;
            }

            _me.transform.position = _jump.Tick(Time.deltaTime);

            if (!_jump.IsFinished) return;

            _isJumping = false;

            TakeLandingPlacement();
            StopCounting();

            transform.position = _jump.End;
            DestroyMe();
        }

        private void TakeLandingPlacement()
        {
            IPlacement previous = _me.CurrentPlacement;

            if (previous != null) previous.OnUnitUnplaced(_me);
            _landing.OnUnitPlaced(_me);
        }
    }
}
