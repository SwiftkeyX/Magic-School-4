using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Travels in a straight line through everyone in its path - the aim target only sets the
    /// direction, it is not a destination, so the shot carries on past whoever it hits first.
    /// </summary>
    internal class PiercingProjectile : Projectile
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
            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients);
            }

            // FLAGGING: pierce projectile never this, so maybe I'll get rid of it?
            ReportHitPosition();
        }

    }
}
