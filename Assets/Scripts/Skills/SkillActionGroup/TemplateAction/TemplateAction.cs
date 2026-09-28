using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// TemplateAction = 1 part of skill that being played independently.
    /// e.g. create AOE, shoot projectile, cast buff/debuff
    /// </summary>
    public abstract class TemplateAction : MonoBehaviour
    {
        // ==================================== universal to template action ====================================
        [SerializeField] protected float _castTime;                 // how long the caster is locked out of auto attacking
        protected ICombatant _me;
        protected List<SkillEffect> _effects;
        protected Hitbox _hitbox;
        protected Vector3 _source;
        protected Vector3 _aimTarget;

        // === OnExpired ===
        internal event Action<SkillStepContext> OnExpired;     // Fire when a template action lifetime runout
        protected float _lifetime;
        private bool _hasExpired;

        // === Rider ===
        private protected Rider _rider;

        // ==================================== from the step before ====================================
        // whatever the previous step produced, this'll be used by next step, to check trigger's condition
        protected SkillStepContext _fromPreviousStep;

        // ==================================== getter ====================================
        public float CastTime => _castTime;
        internal ICombatant Caster => _me;
        internal Vector3 Facing => _aimTarget - _source;    // which direction this template action facing at

        // ==================================== public method ====================================
        // try play template action. if play success, return true.
        // act as factory, since when this function is called, there is no real instance yet.
        public static bool TryPlay(SkillActionGroup group, ICombatant caster,
        Action<SkillStepContext> onExpired = null, Action<SkillStepContext> onHit = null, SkillStepContext fromPreviousStep = null)
        {
            // change skill prefab into scene instace
            TemplateAction instance = Instantiate(group.TemplateAction);

            // config data from previous step
            instance._fromPreviousStep = fromPreviousStep;

            // Apply tuning that specific to this hero
            instance.ApplyTuning(group.Tuning);

            // init skill variable
            if (!instance.TryConfigure(caster, group.Effects, group.Source, group.Target))
            {
                // skill is not play
                Destroy(instance.gameObject);
                return false;
            }

            // other config - each template action wires up the triggers it can actually raise
            instance.SubscribeTriggers(onExpired, onHit);

            // skill is played
            instance.Play();
            return true;
        }

        // Public version of DestroyMe()
        // There's only 1 used: To let Rider called.
        public void EndNow() => DestroyMe();



        // ==================================== abstract method ====================================
        // Each template action child have a different way to resolve how their skill was spawn/aim at.
        // read ResolveSource&ResolveAimTarget in each different's child for more detail
        // returns false if no valid source
        protected abstract bool ResolveSource(ActionSourceEnum source);

        // returns false if no valid target
        protected abstract bool ResolveAimTarget(AimTargetEnum aimTarget);

        // where this template action's instance should sit once source/aim are resolved.
        // e.g. an AOE spawns at the source and point the tip toward the aim target, 
        // e.g.2. projectile spawns at the source and was shooted to aim target.
        protected abstract Vector3 GetSpawnPosition();

        // hard to explain => read each template action
        protected abstract void Play();

        // ...
        protected virtual void ApplyTuning(Tuning tuning)
        {
            if (tuning == null) return;

            if (tuning.CastTime.HasValue) _castTime = tuning.CastTime.Value;
        }


        // ===================================== virtual =====================================
        // To set who is the host of me, the rider.
        // Most actions ride nothing, so doing nothing is the right default
        protected virtual void InitRider() { }

        // life time can be override becuase each template action use different life time
        // e.g. AOE usually have short lifetime, projectile have long lifetime, etc...
        protected virtual void SetLifeTime()
        {
            _lifetime = 0.5f;
            ExpireAfter(_lifetime);
        }

        // ==================================== OnExpired event ====================================
        // Every template action ends the same way => through DestroyMe() or ExpireAfter()
        // to make sure OnExpired always gets fired. 
        // Never call Destroy() on a template action directly.
        protected void DestroyMe()
        {
            // guard 
            if (_me == null) return;

            // guard
            if (_hasExpired) return;
            _hasExpired = true;

            Vector3 expiredPosition = transform.position;

            Destroy(gameObject);

            OnExpired?.Invoke(new SkillStepContext(expiredPosition));
        }

        protected void ExpireAfter(float delay)
        {
            StartCoroutine(ExpireRoutine(delay));
        }

        private IEnumerator ExpireRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            DestroyMe();
        }

        // ==================================== Trigger wiring ====================================
        // Each template action have its own event.
        // e.g. Projectile have OnHit event which is fire when it hit someone
        protected virtual void SubscribeTriggers(Action<SkillStepContext> onExpired, Action<SkillStepContext> onHit)
        {
            // OnExpired is the only trigger every template action can raise 
            // it was fired when the template action dies
            OnExpired += onExpired;
        }


        // ==================================== Hitbox ====================================
        protected void OnTriggerEnter2D(Collider2D other) => _hitbox?.OnTriggerEnter2D(other);
        protected void OnTriggerExit2D(Collider2D other) => _hitbox?.OnTriggerExit2D(other);


        // ==================================== Effect & Recipient ====================================
        // apply effect to the recipients
        protected void ApplyEffectToRecipients(SkillEffect effect, IReadOnlyList<ICombatant> recipients)
        {
            var resolve = ResolveRecipient(effect.Recipient, recipients);
            effect.ApplyEffect(resolve);
        }

        // resolve the new recipient list according to recipientEnum specify
        private IReadOnlyList<IEffectable> ResolveRecipient(EffectRecipientEnum effectRecipientEnum, IReadOnlyList<ICombatant> recipients)
        {
            bool shouldHitMyTeam = false;   // should this effect also hit my team? e.g. buff
            bool shouldHitEnemy = false;    // should this effect hit enemy?

            List<IEffectable> resolve = new List<IEffectable>();
            // this effect hit me only
            if (effectRecipientEnum == EffectRecipientEnum.Self)
            {
                resolve = new List<IEffectable> { _me };
            }

            // this effect hit ally only
            else if (effectRecipientEnum == EffectRecipientEnum.AlliesInPath)
            {
                shouldHitMyTeam = true;
                shouldHitEnemy = false;
            }

            // else if {} ...

            // default setting for all other enum
            else
            {
                shouldHitMyTeam = false;
                shouldHitEnemy = true;
            }

            // resolve a new list
            foreach (var recipient in recipients)
            {
                if (shouldHitEnemy && _me.Team != recipient.Team) resolve.Add(recipient);

                else if (shouldHitMyTeam && _me.Team == recipient.Team) resolve.Add(recipient);
            }

            return resolve;
        }

        // ==================================== helper ====================================
        // try initialize skill, if something went wrong, skill won't play
        private bool TryConfigure(ICombatant caster, List<SkillEffect> effects, ActionSourceEnum source, AimTargetEnum aimTarget)
        {
            // Not Init() before TryPlay(). 
            // because it need to instantiate the new instance first.
            Init(caster, effects);

            // find "source" using source enum
            if (!ResolveSource(source)) return false;

            // find "aim" using aim enum
            if (!ResolveAimTarget(aimTarget)) return false;

            return true;
        }

        private void Init(ICombatant caster, List<SkillEffect> effects)
        {
            _me = caster;
            _effects = effects;
        }

    }

    // Template action but with type casting for Tuning
    public abstract class TemplateAction<TTuning> : TemplateAction where TTuning : Tuning
    {
        protected sealed override void ApplyTuning(Tuning tuning)
        {
            base.ApplyTuning(tuning);
            if (tuning is TTuning typed) ApplyTypedTuning(typed);
        }

        protected abstract void ApplyTypedTuning(TTuning tuning);
    }
}
