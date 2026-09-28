using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    internal class FirstHitProjectile : Projectile
    {
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

        // ======================================== private ==============================================
        private void HandleHit(ICombatant hero)
        {
            // FirstHitProjectile don't hit the same team.
            // NOTE: mention this because PiercingProjectile can hit the same team (to give buff)
            if (hero == null || hero.Team == _me.Team) return;

            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients);
            }

            // tell the next step where this landed, before the destroy
            ReportHitPosition();

            // destroy first hit projectile once it hit target
            DestroyMe();
        }
    }
}