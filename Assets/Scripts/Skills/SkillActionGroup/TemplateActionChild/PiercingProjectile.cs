using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Travels in a straight line through everyone in its path - the aim target only sets the
    /// direction, it is not a destination, so the shot carries on past whoever it hits first.
    /// </summary>
    // Damage falls off for every enemy already gone through; an ally it passes (a buff in its path) is not counted.
    internal class PiercingProjectile : Projectile
    {
        private int _enemiesHit;

        protected override void Play()
        {
            base.Play();
            _enemiesHit = 0;

            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        private void HandleHit(ICombatant hero)
        {
            ReportSkillHit(hero);

            bool isEnemy = hero.Team != _me.Team;
            float damageScale = isEnemy ? Mathf.Pow(1f - _falloff, _enemiesHit) : 1f;
            if (isEnemy) _enemiesHit++;

            List<ICombatant> recipients = new List<ICombatant> { hero };
            foreach (SkillEffect effect in _effects)
            {
                ApplyEffectToRecipients(effect, recipients, damageScale);
            }

            ReportHitPosition();
        }

    }
}
