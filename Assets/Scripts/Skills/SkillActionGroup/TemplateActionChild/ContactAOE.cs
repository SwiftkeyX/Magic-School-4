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

        // ======================================== override ==============================================
        protected override void Play()
        {
            base.Play();

            // initialize hitbox
            OnContactHitbox onceHitbox = new OnContactHitbox();
            onceHitbox.OnHit += HandleAOEHit;
            _hitbox = onceHitbox;
            _hitbox.Init(_me);
        }

        /// <summary>
        /// When a hero who was hit on first contact:
        /// 1) Apply effect once if not cadence
        /// 2) Apply effect over time if cadence
        /// </summary>
        protected override void HandleAOEHit(ICombatant recipient)
        {
            List<ICombatant> recipients = new List<ICombatant> { recipient };
            foreach (SkillEffect effect in _effects)
            {
                // if not cadence, apply once
                if (!effect.Cadence.isCadence)
                {
                    ApplyEffectToRecipients(effect, recipients);
                }

                // if cadence true, apply effect multiple time
                else if (_triggeredOnce.Add((effect, recipient)))
                {
                    // apply effect to each recipient. Each recipients got his own DOT coroutine.
                    if (recipient is MonoBehaviour victim) victim.StartCoroutine(PerHeroCadenceTick(effect, recipient));
                }
            }
        }

        // ======================================== private ==============================================

        // FLAGGING: this is the poison DOT, and it should be its own Status class rather than a
        // coroutine hanging off the AOE that applied it.
        // Per hero cadence tick
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
