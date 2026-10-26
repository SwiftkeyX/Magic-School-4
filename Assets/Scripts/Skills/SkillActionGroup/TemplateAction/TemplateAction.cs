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
    // Every template action ends through DestroyMe() or ExpireAfter(), so OnExpired always fires: never Destroy() one
    // directly. OnExpired is wired here because it is the one trigger every action can raise; each child wires the
    // others it can (a projectile's OnHit). OnSkillHit is raised before the effect is applied. Previous: where the
    // step before expired or its projectile landed (Dwarf's blast spawns where his shot landed). Host: the lead part
    // this one was played Together with (Centaur's hitbox rides his charge). Only a damage effect is scaled by
    // damageScale (a piercing shot's falloff); a heal or buff lands whole.
    public abstract class TemplateAction : MonoBehaviour
    {
        [SerializeField] protected float _castTime;
        protected ICombatant _me;
        protected List<SkillEffect> _effects;
        protected Hitbox _hitbox;
        protected Vector3 _source;
        protected Vector3 _aimTarget;

        protected TemplateActionCallbacks _callbacks = TemplateActionCallbacks.None;
        
        internal event Action<Vector3> OnExpired;     
        protected float _lifetime;
        private bool _hasExpired;

        private protected Rider _rider;

        protected Vector3? _previousPosition;

        protected ICombatant _assignedTarget;

        private protected TemplateAction _host;

        public float CastTime => _castTime;
        internal ICombatant Caster => _me;
        internal Vector3 Facing => _aimTarget - _source;

        public static bool TryPlay(SkillPart group, ICombatant caster,
                                   TemplateActionCallbacks callbacks = null, Vector3? previousPosition = null,
                                   ICombatant assignedTarget = null)
            => Spawn(group, caster, callbacks, previousPosition, assignedTarget, host: null) != null;

        internal static TemplateAction Spawn(SkillPart group, ICombatant caster,
                                             TemplateActionCallbacks callbacks, Vector3? previousPosition,
                                             ICombatant assignedTarget, TemplateAction host)
        {
            TemplateAction instance = Instantiate(group.TemplateAction);

            instance._previousPosition = previousPosition;
            instance._assignedTarget = assignedTarget;
            instance._host = host;

            instance.ApplyTuning(group.Tuning);

            if (!instance.TryConfigure(caster, group.Effects, group.Source, group.Target))
            {
                Destroy(instance.gameObject);
                return null;
            }

            instance._callbacks = callbacks ?? TemplateActionCallbacks.None;
            instance.SubscribeTriggers(instance._callbacks);

            instance.Play();
            return instance;
        }

        public void EndNow() => DestroyMe();


        protected abstract bool ResolveSource(ActionSourceEnum source);

        protected abstract bool ResolveAimTarget(AimTargetEnum aimTarget);

        protected abstract Vector3 GetSpawnPosition();

        protected abstract void Play();

        protected virtual void ApplyTuning(Tuning tuning)
        {
            if (tuning == null) return;

            if (tuning.CastTime.HasValue) _castTime = tuning.CastTime.Value;
        }

        protected virtual void InitRider() { }

        protected virtual void SetLifeTime()
        {
            _lifetime = 0.5f;
            ExpireAfter(_lifetime);
        }

        protected void DestroyMe()
        {
            if (_me == null) return;

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

        protected void ReportSkillHit(ICombatant hero)
        {
            if (hero == null || hero.Team == _me.Team || !hero.IsAlive) return;

            _callbacks.OnSkillHit?.Invoke(hero);
        }

        protected virtual void SubscribeTriggers(TemplateActionCallbacks callbacks)
        {
            OnExpired += callbacks.OnExpired;
        }

        protected void OnTriggerEnter2D(Collider2D other) => _hitbox?.OnTriggerEnter2D(other);
        protected void OnTriggerExit2D(Collider2D other) => _hitbox?.OnTriggerExit2D(other);

        protected void ApplyEffectToRecipients(SkillEffect effect, IReadOnlyList<ICombatant> recipients, float damageScale = 1f)
        {
            var resolve = ResolveRecipient(effect.Recipient, recipients);

            if (effect is AttackSkillEffect damage) damage.ApplyEffect(resolve, _me, damageScale);
            else effect.ApplyEffect(resolve, _me);
        }

        private IReadOnlyList<IEffectable> ResolveRecipient(EffectRecipientEnum effectRecipientEnum, IReadOnlyList<ICombatant> recipients)
        {
            bool shouldHitMyTeam = false;
            bool shouldHitEnemy = false;

            List<IEffectable> resolve = new List<IEffectable>();
            if (effectRecipientEnum == EffectRecipientEnum.Self)
            {
                resolve = new List<IEffectable> { _me };
            }

            else if (effectRecipientEnum == EffectRecipientEnum.AlliesInPath
                  || effectRecipientEnum == EffectRecipientEnum.AlliesInArea)
            {
                shouldHitMyTeam = true;
                shouldHitEnemy = false;
            }

            else
            {
                shouldHitMyTeam = false;
                shouldHitEnemy = true;
            }

            foreach (var recipient in recipients)
            {
                if (shouldHitEnemy && _me.Team != recipient.Team) resolve.Add(recipient);

                else if (shouldHitMyTeam && _me.Team == recipient.Team) resolve.Add(recipient);
            }

            return resolve;
        }

        private bool TryConfigure(ICombatant caster, List<SkillEffect> effects, ActionSourceEnum source, AimTargetEnum aimTarget)
        {
            Init(caster, effects);

            if (!ResolveSource(source)) return false;

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
