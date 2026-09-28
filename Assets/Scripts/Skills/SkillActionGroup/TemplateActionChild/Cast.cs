using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Cast is the template action for instant self-effects (e.g. self-buffs).
    /// No hitbox, no physical footprint - it applies its effects to the caster and is done.
    /// e.g. Bulwark's Guardian's Roar step 1.
    /// </summary>
    public class Cast : TemplateAction
    {
        private ICombatant _target;

        // ======================================= override =======================================
        protected override void Play()
        {
            // how long each effect keeps this cast relevant
            List<float> durations = new List<float>();

            foreach (SkillEffect effect in _effects)
            {
                // if cadence, apply effect over time.
                // e.g. bulwark heal
                if (effect.Cadence.isCadence)
                {
                    // apply effect overtime
                    StartCoroutine(PerHeroCadenceTick(effect, _target));
                }

                // if not cadence, apply effect once.
                else
                {
                    // apply effect once
                    effect.ApplyEffect(new List<IEffectable> { _target });
                }

                durations.Add(GetModifierDuration(effect));
            }

            // After a longest effect duration die, this template action also dies
            // this is to make sure, this template action'll last until the effect really truly dies.
            ExpireAfter(Longest(durations));
        }

        // source mean nothing to Cast.
        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
            }

            // spawn on current target
            else if (source == ActionSourceEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _source = target.transform.position;
            }

            // else if () ...

            // fallback
            else _source = _me.transform.position;

            return true;
        }

        // aim = which unit the cast was targeting
        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            // aim skill at self
            if (aimTarget == AimTargetEnum.Self)
            {
                _target = _me;
            }

            // aim skill at current target
            else if (aimTarget == AimTargetEnum.Current)
            {
                _target = _me.FindCurrentTarget();
                if (_target == null) return false;
            }

            // aim skill at furthest target
            else if (aimTarget == AimTargetEnum.Furthest)
            {
                _target = _me.FindFurthestEnemy(int.MaxValue);
                if (_target == null) return false;
            }

            // else if () ...

            // fallback
            else _target = _me;

            return true;
        }

        protected override Vector3 GetSpawnPosition() => _source;



        // ======================================= private ========================================
        // get duration from this effect's modifier
        private float GetModifierDuration(SkillEffect effect)
        {
            // if isn't modifier, return
            if (!(effect is ModifierSkillEffect modifierEffect)) return 0f;

            // guard
            if (modifierEffect.Modifier == null) return 0f;

            // if cadence, return cadenceDuration
            if (effect.Cadence.isCadence) return effect.Cadence.cadenceDuration;

            // return modifer's duration
            return modifierEffect.Modifier.GetDuration();
        }

        private static float Longest(List<float> durations)
        {
            float longest = 0f;
            foreach (float duration in durations)
            {
                if (duration > longest) longest = duration;
            }

            return longest;
        }

        // FLAGGING: But it should be move later since not all template action need it. maybe to interface?
        private IEnumerator PerHeroCadenceTick(SkillEffect effect, ICombatant hero)
        {
            WaitForSeconds wait = new WaitForSeconds(effect.Cadence.cadenceInterval);
            List<ICombatant> recipients = new List<ICombatant> { hero };
            float elapsed = 0f;

            while (elapsed < effect.Cadence.cadenceDuration)
            {
                yield return wait;
                elapsed += effect.Cadence.cadenceInterval;

                if (hero == null || hero.StateType == HeroStateEnum.Dead) break;
                ApplyEffectToRecipients(effect, recipients);
            }
        }
    }
}
