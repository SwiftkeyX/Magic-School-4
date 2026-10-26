using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    public enum AOEOffsetEnum { Center, Tip }

    /// <summary>
    /// AOE type for TemplateAction
    /// </summary>
    // A sticky AOE follows a Transform, not an ICombatant: a destroyed hero then reads as null. An AOE played Together
    // with a Move rides it (Centaur).
    internal abstract class AOE : TemplateAction<AOETuning>
    {
        protected float _duration = 0.5f;
        private AOEOffsetEnum _offset;
        private Sticky _sticky;
        private int _reachRange = int.MaxValue;

        protected override void ApplyTypedTuning(AOETuning aoeTuning)
        {
            SetShape(aoeTuning.Length, aoeTuning.Width);

            if (aoeTuning.Sticky.HasValue) _sticky.IsSticky = aoeTuning.Sticky.Value;

            if (aoeTuning.Duration.HasValue) _duration = aoeTuning.Duration.Value;

            if (aoeTuning.Offset.HasValue) _offset = aoeTuning.Offset.Value;

            if (aoeTuning.Range.HasValue) _reachRange = aoeTuning.Range.Value;
        }

        protected override void Play()
        {
            InitRider();

            FaceAimTarget();

            transform.position = GetSpawnPosition();

            SetLifeTime();
        }

        protected override void SetLifeTime()
        {
            _lifetime = _duration;
            ExpireAfter(_lifetime);
        }

        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
                if (_sticky.IsSticky) _sticky.Source = _me.transform;
            }

            else if (source == ActionSourceEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _source = target.transform.position;
                if (_sticky.IsSticky) _sticky.Source = target.transform;
            }

            else if (source == ActionSourceEnum.Furthest)
            {
                ICombatant target = _me.FindFurthestEnemy(_reachRange);
                if (target == null) return false;
                _source = target.transform.position;
            }

            else if (source == ActionSourceEnum.WhereProjectileHit)
            {
                if (_previousPosition == null) return false;
                _source = _previousPosition.Value;
                if (_sticky.IsSticky) _sticky.Source = null;
            }

            else if (source == ActionSourceEnum.ClusteredCircle)
            {
                IPlacement target = _me.FindClusteredCircle(_reachRange, HalfWidthAcrossFacing(), isJump: false);
                if (target == null) return false;
                _source = target.transform.position;
            }

            else _source = _me.transform.position;

            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            if (aimTarget == AimTargetEnum.Self)
            {
                _aimTarget = _me.transform.position;
            }

            else if (aimTarget == AimTargetEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            else if (aimTarget == AimTargetEnum.Furthest)
            {
                ICombatant target = _me.FindFurthestEnemy(_reachRange);
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            else if (aimTarget == AimTargetEnum.ClusteredCircle)
            {
                IPlacement target = _me.FindClusteredCircle(_reachRange, HalfWidthAcrossFacing(), isJump: false);
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            else if (aimTarget == AimTargetEnum.ClusteredLaser)
            {
                ICombatant target = _me.FindClusteredLaser(_reachRange, HalfWidthAcrossFacing());
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            else if (aimTarget == AimTargetEnum.WhereProjectileHit)
            {
                if (_previousPosition == null) return false;
                _aimTarget = _previousPosition.Value;
            }

            else _aimTarget = _me.transform.position;

            return true;
        }

        protected override Vector3 GetSpawnPosition()
        {
            if (_offset == AOEOffsetEnum.Center) return _source;

            else if (_offset == AOEOffsetEnum.Tip)
            {
                Vector3 facing = _aimTarget - _source;
                if (facing.sqrMagnitude < 0.0001f) return _source;
                return _source + facing.normalized * HalfLengthAlongFacing();
            }

            return _source;
        }

        protected override void InitRider()
        {
            _rider = Rider.RideOn(this, _host);
        }

        protected abstract void HandleAOEHit(ICombatant recipient);

        protected virtual void Update()
        {
            if (_sticky.IsSticky && _sticky.Source != null) transform.position = _sticky.Source.position;
        }

        private void SetShape(float? length, float? width)
        {
            Vector2 local = LocalShapeSize();
            Vector3 scale = transform.localScale;

            if (length.HasValue && local.y > 0f) scale.y = length.Value / local.y;
            if (width.HasValue && local.x > 0f) scale.x = width.Value / local.x;

            transform.localScale = scale;
        }

        private float HalfLengthAlongFacing() => LocalShapeSize().y * 0.5f * Mathf.Abs(transform.lossyScale.y);
        private float HalfWidthAcrossFacing() => LocalShapeSize().x * 0.5f * Mathf.Abs(transform.lossyScale.x);

        private Vector2 LocalShapeSize()
        {
            Collider2D hitbox = GetComponent<Collider2D>();

            if (hitbox is BoxCollider2D box) return box.size;

            if (hitbox is CircleCollider2D circle) return Vector2.one * circle.radius * 2f;

            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            return sprite == null || sprite.sprite == null ? Vector2.zero : (Vector2)sprite.sprite.bounds.size;
        }

        private void FaceAimTarget()
        {
            if (_rider == null)
            {
                FaceTowards(_aimTarget - _source);
            }

            else
            {
                FaceTowards(_rider.HostFacing);
            }
        }

        private void FaceTowards(Vector3 facing)
        {
            if (facing.sqrMagnitude < 0.0001f) return;

            float degrees = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, degrees + 90f);
        }
    }

}
