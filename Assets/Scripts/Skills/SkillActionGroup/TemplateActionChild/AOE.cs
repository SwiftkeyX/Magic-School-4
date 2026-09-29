using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    public enum AOEOffsetEnum { Center, Tip }

    /// <summary>
    /// AOE type for TemplateAction
    /// </summary>
    internal abstract class AOE : TemplateAction<AOETuning>
    {
        protected float _duration = 0.5f;    // how long the blast stays up before it expires
        private AOEOffsetEnum _offset;
        private Sticky _sticky;
        private int _reachRange = int.MaxValue;   // how far a AOE can reach, in hexes, default to global range (Not a size)

        // ======================================= override =======================================
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

            // resolve AOE's rotation first
            FaceAimTarget();

            // resolve AOE's transform second
            transform.position = GetSpawnPosition();

            SetLifeTime();
        }

        protected override void SetLifeTime()
        {
            _lifetime = _duration;
            ExpireAfter(_lifetime);
        }

        // source = where AOE spawn.
        protected override bool ResolveSource(ActionSourceEnum source)
        {
            // spawn on the caster
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
                if (_sticky.IsSticky) _sticky.Source = _me.transform;
            }

            // spawn on current target
            else if (source == ActionSourceEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _source = target.transform.position;
                if (_sticky.IsSticky) _sticky.Source = target.transform;
            }

            // point at furthest target
            else if (source == ActionSourceEnum.Furthest)
            {
                ICombatant target = _me.FindFurthestEnemy(_reachRange);
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            // spawn on where the previous projectile hit
            // e.g. Solace's dart exploding on impact
            else if (source == ActionSourceEnum.WhereProjectileHit)
            {
                if (_fromPreviousStep?.Position == null) return false;
                _source = _fromPreviousStep.Position.Value;
                if (_sticky.IsSticky) _sticky.Source = null;
            }

            // aim at clustered that measure by specify radius
            else if (source == ActionSourceEnum.ClusteredCircle)
            {
                IPlacement target = _me.FindClusteredCircle(_reachRange, HalfWidthAcrossFacing(), isJump: false);
                if (target == null) return false;
                _source = target.transform.position;
            }

            // else if () ...

            // fallback
            else _source = _me.transform.position;

            return true;
        }

        // aim = where AOE was point at.
        // e.g. ConeAOE's tip point at current enemy
        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            // point at self
            if (aimTarget == AimTargetEnum.Self)
            {
                _aimTarget = _me.transform.position;
            }

            // point at current target
            else if (aimTarget == AimTargetEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            // point at furthest target
            else if (aimTarget == AimTargetEnum.Furthest)
            {
                ICombatant target = _me.FindFurthestEnemy(_reachRange);
                if (target == null) return false;
                _aimTarget = target.transform.position;
            }

            // point at previous projectile hit position
            else if (aimTarget == AimTargetEnum.WhereProjectileHit)
            {
                if (_fromPreviousStep?.Position == null) return false;
                _aimTarget = _fromPreviousStep.Position.Value;
            }

            // else if () ...

            // fallback
            else _aimTarget = _me.transform.position;

            return true;
        }

        // return position where AOE was spawn
        // There's some nuiance => AOE could be place with offset
        // e.g. place the tip of box AOE at user, place the center of circle AOE at user
        protected override Vector3 GetSpawnPosition()
        {
            // the skill's center was place on source 
            if (_offset == AOEOffsetEnum.Center) return _source;

            // the skill's tip was place on source
            else if (_offset == AOEOffsetEnum.Tip)
            {
                Vector3 facing = _aimTarget - _source;
                if (facing.sqrMagnitude < 0.0001f) return _source;
                return _source + facing.normalized * HalfLengthAlongFacing();
            }

            // fallback - an unhandled offset still belongs on the source, not out at (1,1,1)
            return _source;
        }

        // The AOE could rides Move
        // e.g. Centaur's AOE dies when his Move dies
        protected override void InitRider()
        {
            _rider = Rider.FindHostFor(this, _me);
        }

        // ======================================= abstract =======================================
        // When AOE hit someone, apply effect to recipient
        protected abstract void HandleAOEHit(ICombatant recipient);

        // ======================================= virtual =======================================
        protected virtual void Update()
        {
            // if sticky, the AOE should always follow the _source. 
            // Source is a Transform so that a hero dying takes it with them: Unity reports a
            // destroyed object as null, which an ICombatant reference would never have done.
            if (_sticky.IsSticky && _sticky.Source != null) transform.position = _sticky.Source.position;
        }

        // ======================================= private =======================================
        // set width and length of the AOE
        private void SetShape(float? length, float? width)
        {
            Vector2 local = LocalShapeSize();
            Vector3 scale = transform.localScale;

            if (length.HasValue && local.y > 0f) scale.y = length.Value / local.y;
            if (width.HasValue && local.x > 0f) scale.x = width.Value / local.x;

            transform.localScale = scale;
        }

        // half this shape's length or width
        // e.g. box 2 wide x 10 long    => half length = 5, half width = 1
        //      circle radius = 3       => half legnth/widht = 1.5
        private float HalfLengthAlongFacing() => LocalShapeSize().y * 0.5f * Mathf.Abs(transform.lossyScale.y);
        private float HalfWidthAcrossFacing() => LocalShapeSize().x * 0.5f * Mathf.Abs(transform.lossyScale.x);

        // the original size of the shape before it was set 
        private Vector2 LocalShapeSize()
        {
            Collider2D hitbox = GetComponent<Collider2D>();

            // BOXAOE
            if (hitbox is BoxCollider2D box) return box.size;

            // CIRCLEAOE
            if (hitbox is CircleCollider2D circle) return Vector2.one * circle.radius * 2f;

            // else if {} ...

            // fallback
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            return sprite == null || sprite.sprite == null ? Vector2.zero : (Vector2)sprite.sprite.bounds.size;
        }

        // Point the AOE tip toward aim target
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
