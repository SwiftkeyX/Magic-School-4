using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    // It follows its target every frame so it never misses. Aimed at a hex rather than a hero it may collide with
    // nothing, so arriving counts as the hit.
    internal class HomingProjectile : Projectile
    {
        private const float ArrivalDistance = 0.15f;

        protected override void Play()
        {
            base.Play();

            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        protected override void Update()
        {
            GetAimDirection();

            base.Update();

            ExplodeOnArrival();
        }

        private void ExplodeOnArrival()
        {
            if (_target != null || _aimAt == null) return;

            if (Vector3.Distance(transform.position, _aimAt.position) > ArrivalDistance) return;

            ReportHitPosition();
            DestroyMe();
        }

        private void HandleHit(ICombatant hero)
        {
            if (hero != _target) return;

            ReportSkillHit(hero);

            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients);
            }

            ReportHitPosition();

            DestroyMe();
        }

    }
}
