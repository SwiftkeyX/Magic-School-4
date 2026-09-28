using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// ZoneAOE are template action that apply effect over time.
    /// Effect here was apply to the recipients on standing in the effect, if they walk out of it, they don't get effect re-apply.
    /// e.g. Roland
    /// </summary>
    internal class ZoneAOE : AOE
    {
        private float _interval;
        const float VALUENOTASSIGN = -1f;

        // ======================================== override ==============================================
        protected override void Play()
        {
            // FLAGGING: This shouldn't be check everytime, it should be initialize somewhere once.
            // Read cadence BEFORE base.Play()
            ResolveCadence();

            base.Play();

            // initialize hitbox
            OnTickHitbox hitbox = new OnTickHitbox();
            hitbox.OnHit += HandleAOEHit;
            hitbox.Init(_me);
            _hitbox = hitbox;

            // start applying effect every interval
            StartCoroutine(CadenceTick(hitbox));
        }

        // ZoneAOE have _lifetime as the same to cadence duration
        protected override void SetLifeTime()
        {
            // guard
            if (_duration == VALUENOTASSIGN) { base.SetLifeTime(); return; }

            _lifetime = _duration;
            ExpireAfter(_lifetime);
        }

        // a hero currently in the zone got ticked - apply every cadence effect to them
        protected override void HandleAOEHit(ICombatant recipient)
        {
            List<ICombatant> recipients = new List<ICombatant> { recipient };
            foreach (SkillEffect effect in _effects)
            {
                if (effect.Cadence.isCadence) ApplyEffectToRecipients(effect, recipients);
            }
        }

        // ======================================== private ==============================================
        // Assign value to cadence interval, and cadence duration.
        // To guard - cadence effects on this zone must share one interval
        // FLAGGING: cadenceDuration override the _duration that maybe was tuned. I don't sure if this a problem.
        private void ResolveCadence()
        {
            _interval = VALUENOTASSIGN;
            _duration = VALUENOTASSIGN;
            foreach (SkillEffect effect in _effects)
            {
                if (!effect.Cadence.isCadence) continue;

                // if interval doesn't assign yet, assign it value
                if (_interval == VALUENOTASSIGN)
                {
                    _interval = effect.Cadence.cadenceInterval;
                    _duration = effect.Cadence.cadenceDuration;
                }

                // if interval/duration value is assign already, we don't want 2 value, so log error.
                else
                {
                    if (effect.Cadence.cadenceInterval != _interval)
                        Debug.LogError($"{name}: cadence effects on the same zone must share one interval, found {_interval} and {effect.Cadence.cadenceInterval} - only {_interval} will be used", this);

                    if (effect.Cadence.cadenceDuration != _duration)
                        Debug.LogError($"{name}: cadence effects on the same zone must share one duration, found {_duration} and {effect.Cadence.cadenceDuration} - only {_duration} will be used", this);
                }
            }
        }

        // Global cadence tick - on a fixed schedule from spawn, no initial collision needed to start ticking.
        private IEnumerator CadenceTick(OnTickHitbox hitbox)
        {
            WaitForSeconds wait = new WaitForSeconds(_interval);

            while (true)
            {
                yield return wait;
                hitbox.FireTick();
            }
        }
    }
}
