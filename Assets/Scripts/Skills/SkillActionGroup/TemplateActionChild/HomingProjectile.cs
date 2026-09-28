using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    internal class HomingProjectile : Projectile
    {
        private const float ArrivalDistance = 0.15f;

        // ======================================== override method ==============================================
        protected override void Play()
        {
            base.Play();

            // initialize hitbox: apply effects once, on first contact
            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        protected override void Update()
        {
            // homing projectile update target position every frame, so it never miss on target enemy
            GetAimDirection();

            base.Update();

            ExplodeOnArrival();
        }

        // Context: HomingProjectile could target the hex instead of hero, so sometime it don't collide with anything 
        // Resolve that by counting the projectile arrival as collision
        private void ExplodeOnArrival()
        {
            if (_target != null || _aimAt == null) return;

            if (Vector3.Distance(transform.position, _aimAt.position) > ArrivalDistance) return;

            ReportHitPosition();
            DestroyMe();
        }

        // ======================================== private ==============================================

        private void HandleHit(ICombatant hero)
        {
            // if hero is hit isn't the target one, don't apply effect
            if (hero != _target) return;

            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients);
            }

            // tell the next step where this landed, before the destroy
            ReportHitPosition();

            // destroy homing projectile once it hit target
            DestroyMe();
        }

    }
}
