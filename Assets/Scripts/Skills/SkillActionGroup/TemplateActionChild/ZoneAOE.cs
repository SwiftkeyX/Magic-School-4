using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// ZoneAOE are template action that apply effect over time.
    /// Effect here was apply to the recipients on standing in the effect, if they walk out of it, they don't get effect re-apply.
    /// e.g. Orc Blademaster
    /// 
    /// NOTE: ZonAOE MUST be pair with (cadence = true) effect. If the effect isn't cadence, the ZoneAOE don't do anything.
    /// </summary>
    // Ticks on a fixed schedule from spawn, no first collision needed. Its cadence effects must share one interval,
    // and its lifetime is the cadence duration.
    internal class ZoneAOE : AOE
    {
        private float _interval;
        const float VALUENOTASSIGN = -1f;

        protected override void Play()
        {
            ResolveCadence();

            base.Play();

            OnTickHitbox hitbox = new OnTickHitbox();
            hitbox.OnHit += HandleAOEHit;
            hitbox.Init(_me);
            _hitbox = hitbox;

            StartCoroutine(CadenceTick(hitbox));
        }

        protected override void SetLifeTime()
        {
            if (_duration == VALUENOTASSIGN) { base.SetLifeTime(); return; }

            _lifetime = _duration;
        }

        protected override void HandleAOEHit(ICombatant recipient)
        {
            ReportSkillHit(recipient);

            List<ICombatant> recipients = new List<ICombatant> { recipient };
            foreach (SkillEffect effect in _effects)
            {
                if (effect.Cadence.isCadence) ApplyEffectToRecipients(effect, recipients);
            }
        }

        private void ResolveCadence()
        {
            _interval = VALUENOTASSIGN;
            _duration = VALUENOTASSIGN;
            foreach (SkillEffect effect in _effects)
            {
                if (!effect.Cadence.isCadence) continue;

                if (_interval == VALUENOTASSIGN)
                {
                    _interval = effect.Cadence.cadenceInterval;
                    _duration = effect.Cadence.cadenceDuration;
                }

                else
                {
                    if (effect.Cadence.cadenceInterval != _interval)
                        Debug.LogError($"{name}: cadence effects on the same zone must share one interval, found {_interval} and {effect.Cadence.cadenceInterval} - only {_interval} will be used", this);

                    if (effect.Cadence.cadenceDuration != _duration)
                        Debug.LogError($"{name}: cadence effects on the same zone must share one duration, found {_duration} and {effect.Cadence.cadenceDuration} - only {_duration} will be used", this);
                }
            }
        }

        private IEnumerator CadenceTick(OnTickHitbox hitbox)
        {
            if (_interval == VALUENOTASSIGN) yield break;

            WaitForSeconds wait = new WaitForSeconds(_interval);
            int ticks = Mathf.Max(1, Mathf.RoundToInt(_duration / _interval));

            for (int i = 0; i < ticks; i++)
            {
                yield return wait;
                hitbox.FireTick();
            }

            DestroyMe();
        }
    }
}
