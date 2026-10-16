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
        private int _enemiesHit;    // enemies this shot already went through, for the damage falloff

        // ======================================== override method ==============================================
        protected override void Play()
        {
            base.Play();
            _enemiesHit = 0;

            // initialize hitbox: apply effects once, on first contact
            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }


        // ======================================== private ==============================================
        private void HandleHit(ICombatant hero)
        {
            ReportSkillHit(hero);

            // the damage falls off for every enemy the shot already went through: 0.33 => 100%, 67%, 45%, ...
            // An ally it passes (a buff in its path) is not counted and takes nothing off.
            bool isEnemy = hero.Team != _me.Team;
            float damageScale = isEnemy ? Mathf.Pow(1f - _falloff, _enemiesHit) : 1f;
            if (isEnemy) _enemiesHit++;

            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients, damageScale);
            }

            // FLAGGING: pierce projectile never this, so maybe I'll get rid of it?
            ReportHitPosition();
        }

    }
}
