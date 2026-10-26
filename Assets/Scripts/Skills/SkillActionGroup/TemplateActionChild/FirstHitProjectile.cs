using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    // Never hits its own team (a PiercingProjectile can, to give a buff).
    internal class FirstHitProjectile : Projectile
    {
        protected override void Play()
        {
            base.Play();

            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        private void HandleHit(ICombatant hero)
        {
            if (hero == null || hero.Team == _me.Team) return;

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