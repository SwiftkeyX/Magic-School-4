using System;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Projectile was aim toward the "target", what determined which recipients get hit is based on the projectile type.
    /// </summary>
    // Size 1 means as drawn and touches nothing: it used to be written onto both axes, flattening every prefab (the
    // bolts are 0.1 by 0.3) to the same 1x1 square. Any other size is set outright, so the projectile comes out
    // square. A target that dies first leaves the projectile flying on.
    public abstract class Projectile : TemplateAction<ProjectileTuning>
    {
        const float PROJECTILELIFETIME = 10f;
        protected Vector3 _direction;
        protected ICombatant _target;
        protected Transform _aimAt;

        [SerializeField] protected float _speed = 8f;
        protected float _size = 1f;
        protected int _reachRange = int.MaxValue;
        protected float _spread = 1f;
        protected float _falloff = 0f;

        private event Action<Vector3> OnHit;
        private bool _hasReportedHit;

        protected override void SubscribeTriggers(TemplateActionCallbacks callbacks)
        {
            base.SubscribeTriggers(callbacks);

            OnHit += callbacks.OnHit;
        }

        protected void ReportHitPosition()
        {
            if (_hasReportedHit) return;
            _hasReportedHit = true;

            OnHit?.Invoke(transform.position);
        }

        protected bool IsTargetAlive => _target != null && _target.IsAlive;

        protected override void ApplyTypedTuning(ProjectileTuning projectileTuning)
        {
            if (projectileTuning.Range.HasValue) _reachRange = projectileTuning.Range.Value;
            if (projectileTuning.Spread.HasValue) _spread = projectileTuning.Spread.Value;
            if (projectileTuning.Size.HasValue) _size = projectileTuning.Size.Value;
            if (projectileTuning.Falloff.HasValue) _falloff = Mathf.Clamp01(projectileTuning.Falloff.Value);
            if (projectileTuning.Speed.HasValue) _speed = Mathf.Max(0.1f, projectileTuning.Speed.Value);
        }

        protected override void Play()
        {
            transform.position = GetSpawnPosition();

            SetProjectileSize(_size);

            GetAimDirection();

            FaceAimTarget();

            SetLifeTime();
        }

        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
            }

            else _source = _me.transform.position;

            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            if (aimTarget == AimTargetEnum.Self)
            {
                Debug.LogWarning("[Scriptable Object] Projectile can't be aim at self", this);
                return false;
            }

            else if (aimTarget == AimTargetEnum.Current)
            {
                _target = _me.FindCurrentTarget();
                if (_target == null) return false;

                _aimAt = _target.transform;
            }

            else if (aimTarget == AimTargetEnum.Furthest)
            {
                _target = _me.FindFurthestEnemy(_reachRange);
                if (_target == null) return false;

                _aimAt = _target.transform;
            }

            else if (aimTarget == AimTargetEnum.ClusteredLaser)
            {
                _target = _me.FindClusteredLaser(_reachRange, _spread);
                if (_target == null) return false;

                _aimAt = _target.transform;
            }

            else if (aimTarget == AimTargetEnum.ClusteredCircle)
            {
                IPlacement blastCentre = _me.FindClusteredCircle(_reachRange, _spread, isJump: false);
                if (blastCentre == null) return false;

                _aimAt = blastCentre.transform;
            }

            else if (aimTarget == AimTargetEnum.Assigned)
            {
                _target = _assignedTarget;
                if (_target == null) return false;

                _aimAt = _target.transform;
            }

            else
            {
                Debug.LogWarning($"[Scriptable Object] Projectile can't aim at {aimTarget}", this);
                return false;
            }

            return true;
        }

        protected override Vector3 GetSpawnPosition() => _source;

        protected override void SetLifeTime()
        {
            _lifetime = PROJECTILELIFETIME;
            ExpireAfter(_lifetime);
        }

        protected virtual void GetAimDirection()
        {
            if (_target != null && !IsTargetAlive) return;

            if (_aimAt == null) return;

            _aimTarget = _aimAt.position;

            _direction = (_aimTarget - transform.position).normalized;

            if (_direction == Vector3.zero)
            {
                DestroyMe();
            }
        }

        protected virtual void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private const float Default = 1f;

        private void SetProjectileSize(float size = Default)
        {
            if (size == Default) return;

            this.transform.localScale = new Vector3(size, size, this.transform.localScale.z);
        }

        private void FaceAimTarget()
        {
            Vector3 facing = _aimTarget - _source;
            if (facing.sqrMagnitude < 0.0001f) return;

            float degrees = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, degrees + 90f);
        }
    }
}
