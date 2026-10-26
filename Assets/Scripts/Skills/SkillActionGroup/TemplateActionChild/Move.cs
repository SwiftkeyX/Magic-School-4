using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Engine;

namespace MagicSchool.Skills
{
    internal class Move : TemplateAction<MoveTuning>
    {
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private int _jumpRange = 2;
        private float _jumpDuration = 0.5f;
        private float _spread = 1f;
        private bool _maxRange;         // read FindClusteredCharge() in FindTarget.cs
        private IPlacement _landing;    // hex the caster ends up on after moving
        private CurveMotion _jump;
        private bool _isJumping;

        // every caster a Move is moving right now, with how many: its Cast state holds until it lands (See IsMoving)
        private static readonly Dictionary<ICombatant, int> Moving = new Dictionary<ICombatant, int>();
        private bool _isCounted;

        // a skill is still moving this caster: its Cast state must not end yet, or it would start a step mid-air, and that
        // step would carry it back to where it jumped from once it landed
        // e.g.     Knight's leap, longer than his cast time
        internal static bool IsMoving(ICombatant caster) => caster != null && Moving.TryGetValue(caster, out int n) && n > 0;

        // ======================================== override ========================================
        protected override void ApplyTypedTuning(MoveTuning moveTuning)
        {
            if (moveTuning.Range.HasValue) _jumpRange = moveTuning.Range.Value;
            if (moveTuning.Duration.HasValue) _jumpDuration = moveTuning.Duration.Value;
            if (moveTuning.Spread.HasValue) _spread = moveTuning.Spread.Value;
            if (moveTuning.MaxRange.HasValue) _maxRange = moveTuning.MaxRange.Value;
        }

        protected override void Play()
        {
            // this instance is only a driver, so park it on the caster - it never gets seen
            transform.position = GetSpawnPosition();

            // Reserved the landing hex before the actual jump
            // this ensure nobody else take our landing hex
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

        // source = the hero that will be moved
        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
            }

            // else if {} ...

            // FLAGGING: move template action are currently only work with self 
            // fallback
            else
            {
                _source = _me.transform.position;
                return false;
            }

            return true;
        }

        // aim = where the source will be moved at
        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            // source move at a hex which is most clustered with enemies
            if (aimTarget == AimTargetEnum.ClusteredCircle)
            {
                // ask for the hex to land on.
                _landing = _me.FindClusteredCircle(_jumpRange, _spread, isJump: true);
                if (_landing == null) return false;     // nothing in reach is worth jumping to, so don't cast

                _aimTarget = _landing.transform.position;
                return true;
            }

            else if (aimTarget == AimTargetEnum.ClusteredLaser)
            {
                _landing = _me.FindClusteredCharge(_jumpRange, _spread, _maxRange);
                if (_landing == null) return false;     // nothing worth charging at, so don't cast

                _aimTarget = _landing.transform.position;
                return true;
            }

            // source move at a random free hex in reach
            else if (aimTarget == AimTargetEnum.Random)
            {
                _landing = _me.FindRandomFreeHex(_jumpRange);
                if (_landing == null) return false;

                _aimTarget = _landing.transform.position;
                return true;
            }

            // else if () ...

            // no landing spot means no jump - falling back to "move to where I already am" would
            // burn the cast on nothing
            Debug.LogWarning($"[Scriptable Object] Move can't aim at {aimTarget}", this);
            return false;
        }

        protected override Vector3 GetSpawnPosition() => _source;

        // Landing is what ends this action, not a timer - see Update(). The number is still
        // recorded so anything reading _lifetime sees how long the jump actually takes.
        protected override void SetLifeTime()
        {
            _lifetime = _jumpDuration;
        }

        // landed, or gone any other way (the caster sold, the board cleared): no longer moving him
        private void OnDestroy() => StopCounting();

        private void StopCounting()
        {
            if (!_isCounted) return;
            _isCounted = false;

            if (Moving.TryGetValue(_me, out int n) && n > 1) Moving[_me] = n - 1;
            else Moving.Remove(_me);
        }

        // ======================================== private ========================================
        private void Update()
        {
            if (!_isJumping) return;

            // the caster was destroyed mid-move (e.g. a benchmark lane cleared). An interface's == null does not
            // see a destroyed Unity object, so ask the object itself. Destroy without DestroyMe: OnExpired would
            // play the landing part for a caster that no longer exists.
            if (_me is Object caster && caster == null)
            {
                _isJumping = false;
                Destroy(gameObject);
                return;
            }

            _me.transform.position = _jump.Tick(Time.deltaTime);

            if (!_jump.IsFinished) return;

            _isJumping = false;

            // after finish landing, set current placement there
            TakeLandingPlacement();
            StopCounting();

            // die where the caster landed, so OnExpired hands the "landing spot" to the next step
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
