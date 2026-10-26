using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// ContactAOE are template action that can apply effect to contacted hero => can apply both "once" or "overtime".
    /// 1) If apply once, it mean to apply effect to target immediately (apply at first contact).
    ///
    /// 2) If apply overtime, it mean after first contact, it still apply damage over time to them afterward like a poison.
    /// Herores who get poison will can't walk out of poison like ZoneAOE, but they get full damage duration instead.
    /// 
    /// Example
    /// e.g. Pip
    /// </summary>
    internal class ContactAOE : AOE
    {
        private readonly HashSet<(SkillEffect effect, ICombatant hero)> _triggeredOnce = new HashSet<(SkillEffect, ICombatant)>();

        protected override void Play()
        {
            base.Play();

            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleAOEHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        protected override void HandleAOEHit(ICombatant recipient)
        {
            ReportSkillHit(recipient);

            List<ICombatant> recipients = new List<ICombatant> { recipient };
            foreach (SkillEffect effect in _effects)
            {
                if (!effect.Cadence.isCadence)
                {
                    ApplyEffectToRecipients(effect, recipients);
                }

                else if (_triggeredOnce.Add((effect, recipient)))
                {
                    if (recipient is MonoBehaviour victim) victim.StartCoroutine(PerHeroCadenceTick(effect, recipient));
                }
            }
        }

        private IEnumerator PerHeroCadenceTick(SkillEffect effect, ICombatant hero)
        {
            WaitForSeconds wait = new WaitForSeconds(effect.Cadence.cadenceInterval);
            List<ICombatant> recipients = new List<ICombatant> { hero };
            float elapsed = 0f;

            while (elapsed < effect.Cadence.cadenceDuration)
            {
                yield return wait;
                elapsed += effect.Cadence.cadenceInterval;

                if (hero == null || hero.StateType == HeroStateEnum.Dead) yield break;
                ApplyEffectToRecipients(effect, recipients);
            }
        }
    }
}
