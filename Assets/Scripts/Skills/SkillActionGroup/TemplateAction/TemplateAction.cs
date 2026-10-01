using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// TemplateAction = the skill prefab that actually appears on the board and does the work.
    /// Example,    
    ///         the projectile prefab that flies toward target
    ///         the AOE prefab that spawn on the ground
    ///         the move prefab that carries the caster forward
    ///
    /// The template action itself knows how to behave
    /// Example,
    ///         projectile - know how to fly toward the target, what it collides with, when it dies 
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

        // === Callbacks ===
        // what this action calls back to the skill that played it
        protected TemplateActionCallbacks _callbacks = TemplateActionCallbacks.None;
        
        // === OnExpired ===
        // Fire when a template action lifetime runout
        internal event Action<Vector3> OnExpired;     
        protected float _lifetime;
        private bool _hasExpired;

        // === Rider ===
        private protected Rider _rider;

        // ==================================== context (handed in by whoever played this template action) ====================================
        // where the step before this one expired, or where its projectile landed. Null on a first step.
        // e.g.     Dwarf's blast spawns where his shot landed
        protected Vector3? _previousPosition;

        // who this action was told to aim at.
        // e.g.     FireTimingRunner picks a random enemy for each shot it fires
        protected ICombatant _assignedTarget;

        // ==================================== getter ====================================
        public float CastTime => _castTime;
        internal ICombatant Caster => _me;
        internal Vector3 Facing => _aimTarget - _source;    // which direction this template action facing at

        // ==================================== public method ====================================
        // try play template action. if play success, return true.
        // act as factory, since when this function is called, there is no real instance yet.
        public static bool TryPlay(SkillPart group, ICombatant caster,
                                   TemplateActionCallbacks callbacks = null, Vector3? previousPosition = null,
                                   ICombatant assignedTarget = null)
        {
            // change skill prefab into scene instace
            TemplateAction instance = Instantiate(group.TemplateAction);

            // config data from previous guy
            instance._previousPosition = previousPosition;
            instance._assignedTarget = assignedTarget;

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
            instance._callbacks = callbacks ?? TemplateActionCallbacks.None;
            instance.SubscribeTriggers(instance._callbacks);

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

            OnExpired?.Invoke(expiredPosition);
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

        // ==================================== OnSkillHit event ====================================
        // If this template action hit someone, invoke onSkillHit, BEFORE the effect is applied.
        // e.g. Projectile hit enemy, ...
        protected void ReportSkillHit(ICombatant hero)
        {
            if (hero == null || hero.Team == _me.Team || !hero.IsAlive) return;

            _callbacks.OnSkillHit?.Invoke(hero);
        }

        // ==================================== Trigger wiring ====================================
        // Each template action have its own event.
        // e.g. Projectile have OnHit event which is fire when it hit someone
        protected virtual void SubscribeTriggers(TemplateActionCallbacks callbacks)
        {
            // OnExpired is wired on the base class 
            // because OnExpired the only trigger every template action can raise 
            OnExpired += callbacks.OnExpired;
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
            else if (effectRecipientEnum == EffectRecipientEnum.AlliesInPath
                  || effectRecipientEnum == EffectRecipientEnum.AlliesInArea)
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
