using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{
    /// <summary>
    /// Cast is the template action for instant self-effects (e.g. self-buffs).
    /// No hitbox, no physical footprint - it applies its effects to the caster and is done.
    /// e.g. Shield Knight's Guardian's Roar step 1.
    /// </summary>
    // A Cast lives as long as its longest effect: a cadence effect ticks on the Cast itself, so destroying it early
    // would cut the effect off. That is also why a Cast holding a ManaBlocked modifier keeps its caster from gaining
    // mana for that long (Swordsman's charge).
    public class Cast : TemplateAction
    {
        private ICombatant _target;
        private int _cadencesRunning;

        protected override void Play()
        {
            List<float> durations = new List<float>();

            foreach (SkillEffect effect in _effects)
            {
                if (effect.Cadence.isCadence)
                {
                    _cadencesRunning++;
                    StartCoroutine(PerHeroCadenceTick(effect, _target));
                }

                else
                {
                    effect.ApplyEffect(new List<IEffectable> { _target }, _me);
                }

                durations.Add(GetDuration(effect));
            }

            StartCoroutine(ExpireWhenEffectsEnd(Longest(durations)));
        }

        protected override bool ResolveSource(ActionSourceEnum source)
        {
            if (source == ActionSourceEnum.Self)
            {
                _source = _me.transform.position;
            }

            else if (source == ActionSourceEnum.Current)
            {
                ICombatant target = _me.FindCurrentTarget();
                if (target == null) return false;
                _source = target.transform.position;
            }

            else _source = _me.transform.position;

            return true;
        }

        protected override bool ResolveAimTarget(AimTargetEnum aimTarget)
        {
            if (aimTarget == AimTargetEnum.Self)
            {
                _target = _me;
            }

            else if (aimTarget == AimTargetEnum.Current)
            {
                _target = _me.FindCurrentTarget();
                if (_target == null) return false;
            }

            else if (aimTarget == AimTargetEnum.Furthest)
            {
                _target = _me.FindFurthestEnemy(int.MaxValue);
                if (_target == null) return false;
            }

            else if (aimTarget == AimTargetEnum.Assigned)
            {
                _target = _assignedTarget;
                if (_target == null) return false;
            }

            else _target = _me;

            return true;
        }

        protected override Vector3 GetSpawnPosition() => _source;


        private float GetDuration(SkillEffect effect)
        {
            if (effect.Cadence.isCadence) return effect.Cadence.Duration;

            if (effect is ModifierSkillEffect modifierEffect) return modifierEffect.Modifier.GetDuration();

            return 0f;
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

        private IEnumerator PerHeroCadenceTick(SkillEffect effect, ICombatant hero)
        {
            WaitForSeconds wait = new WaitForSeconds(effect.Cadence.cadenceInterval);
            List<ICombatant> recipients = new List<ICombatant> { hero };
            float elapsed = 0f;

            while (elapsed < effect.Cadence.Duration)
            {
                yield return wait;
                elapsed += effect.Cadence.cadenceInterval;

                if (hero == null || hero.StateType == HeroStateEnum.Dead) break;
                ApplyEffectToRecipients(effect, recipients);
            }

            _cadencesRunning--;
        }

        private IEnumerator ExpireWhenEffectsEnd(float longest)
        {
            yield return new WaitForSeconds(longest);

            while (_cadencesRunning > 0) yield return null;

            DestroyMe();
        }
    }
}
