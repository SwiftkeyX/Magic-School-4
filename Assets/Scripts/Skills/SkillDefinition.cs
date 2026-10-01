using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// <summary>
    /// A whole container for 1 skill.
    /// SkillDefinition contain list of SkillStep for both passive & active skill.
    /// skill are separated into step, those step are working together in order, to create a actual skill.
    /// </summary>
    public abstract class SkillDefinition
    {
        private TemplateActionRegistrySO _registry;

        // active skill.
        public IReadOnlyList<SkillStep> ActiveSteps { get; private set; } = new List<SkillStep>();

        // passive skill.
        public IReadOnlyList<SkillStep> PassiveSteps { get; private set; } = new List<SkillStep>();

        protected ICombatant Caster { get; private set; }   // owner of this skill
        public float CastTime { get; private set; } // the animation's duration to play this specific skill

        protected SkillDefinition(TemplateActionRegistrySO registry)
        {
            _registry = registry;
        }

        // ============================================== init ==============================================
        /// Active and Passive skill of the hero
        /// Called once from Init
        protected abstract List<SkillStep> Active(TemplateActionRegistrySO registry);
        protected virtual List<SkillStep> Passive(TemplateActionRegistrySO registry) => new List<SkillStep>();

        public virtual void Init(ICombatant caster)
        {
            Caster = caster;

            if (_registry != null)
            {
                ActiveSteps = Active(_registry) ?? new List<SkillStep>();
                PassiveSteps = Passive(_registry) ?? new List<SkillStep>();

                // prevent second init from rebuilt the skill
                _registry = null;
            }

            foreach (SkillStep step in ActiveSteps) step.Init(caster);

            foreach (SkillStep step in PassiveSteps) step.Init(caster);
        }

        // ============================================== virtual ==============================================
        public abstract string SkillName { get; }
        public abstract string Description { get; }
        public virtual string PassiveDescription => "";
        public virtual bool HasActive => ActiveSteps.Count > 0;
        public virtual bool HasPassive => PassiveSteps.Count > 0;

        // ============================================== virtual hooks ==============================================
        // If mana is full, play the active skill. 
        // Returns whether anything played.
        public virtual bool OnCast()
        {
            if (!HasActive || ActiveSteps[0].Trigger != TriggerEnum.OnCast) return false;

            return PlayStep(ActiveSteps, 0);
        }

        // If auto-attack, play OnAttack passive. 
        // Returns whether anything played.
        public virtual bool OnAttack(ICombatant target) => PlayPassive(TriggerEnum.OnAttack);

        // At combat start, play OnCombat passive.
        // Returns whether anything played.
        public virtual bool OnCombatStart() => PlayPassive(TriggerEnum.OnCombatStart);

        // if any hero dies, play OnHeroDied passive.
        // e.g. Troll gains health whenever an enemy dies
        public virtual void OnHeroDied(ICombatant dead) { }

        // add other hook
        // ... 

        // ============================================== private ==============================================
        // FIXLATER: passive step sound dump. Could we pair passive with dictionary instead.
        // e.g. dict.TryGet(OnAttack)
        // play the passive that starts with this trigger, if there is one
        private bool PlayPassive(TriggerEnum trigger)
        {
            if (PassiveSteps.Count == 0 || PassiveSteps[0].Trigger != trigger) return false;

            return PlayStep(PassiveSteps, 0);
        }

        // Play one step of a skill chain
        private bool PlayStep(IReadOnlyList<SkillStep> steps, int stepIndex, Vector3? previousPosition = null)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count) return false;

            // ASKING: I don't like how the rider was now use.
            // I think the problem come from the skillstep itself.
            // the skillstep itself was flexible but not intutive to understand by the reader.
            // so how about changing it? instead of playing it into step. how about using the existing hook instead.
            // e.g.     now: step 1 => 2 => 3
            //          new: OnCastStart (could have than 1 template action) => OnHit (if exist) => OnExpired (if exist)
            int riderIndex = stepIndex + 1;
            bool hasRider = riderIndex < steps.Count && steps[riderIndex].Trigger == TriggerEnum.OnCastStart;
            int nextIndex = hasRider ? riderIndex + 1 : riderIndex;

            // init the callback
            Action<Vector3> onExpired = NextStep(steps, nextIndex, TriggerEnum.OnExpired);
            Action<Vector3> onHit = NextStep(steps, nextIndex, TriggerEnum.OnHit);

            // play the first action group that met the condition
            foreach (SkillActionGroup group in steps[stepIndex].ActionGroups)
            {
                if (SkillCondition.Ask(group.Conditions, Caster) == ConditionResultEnum.ConditionIsNotMet) continue;

                var callbacks = new TemplateActionCallbacks
                {
                    OnExpired = onExpired,
                    OnHit = onHit,
                    OnSkillHit = group.OnSkillHit,
                };

                // 1) play the template action, continue if condition isn't met 
                // 2) give callback to the template action
                if (!Play(group, callbacks, previousPosition)) continue;

                // ...
                if (hasRider) PlayRider(steps[riderIndex], previousPosition);

                return true;
            }

            return false;
        }

        // Play a rider step. It chains nothing: the step it rides on is the one that carries the chain.
        private void PlayRider(SkillStep rider, Vector3? previousPosition)
        {
            foreach (SkillActionGroup group in rider.ActionGroups)
            {
                if (SkillCondition.Ask(group.Conditions, Caster) == ConditionResultEnum.ConditionIsNotMet) continue;

                var callbacks = new TemplateActionCallbacks { OnSkillHit = group.OnSkillHit };

                if (Play(group, callbacks, previousPosition)) return;
            }
        }

        // Play a single group on its own.
        // no step, no chain skill. 
        protected bool PlayGroup(SkillActionGroup group, ICombatant assignedTarget = null)
            => Play(
                group, 
                new TemplateActionCallbacks { OnSkillHit = group.OnSkillHit }, 
                null,
                assignedTarget
            );

        // try play template action
        // wiring NextStep() callback to template action
        private bool Play(SkillActionGroup group, TemplateActionCallbacks callbacks, Vector3? previousPosition,
                          ICombatant assignedTarget = null)
        {
            // guard
            if (Caster is UnityEngine.Object hero && hero == null) return false;

            // try play template action and wiring callback
            if (!TemplateAction.TryPlay(group, Caster, callbacks, previousPosition, assignedTarget)) return false;

            CastTime = group.Tuning?.CastTime ?? group.TemplateAction.CastTime;
            return true;
        }

        // return the callback that can be use to fires the next steps
        // the callback will be used by the template action. the example usage of this callback:
        // e.g.     the AOE is gone (the prefab is destroyed)   =   call NextStep(OnExpired)
        //          the moment projectile hit something         =   call NextStep(OnHit)
        private Action<Vector3> NextStep(IReadOnlyList<SkillStep> steps, int nextIndex, TriggerEnum trigger)
        {
            if (nextIndex >= steps.Count) return null;

            if (steps[nextIndex].Trigger != trigger) return null;

            return position => PlayStep(steps, nextIndex, position);
        }
    }
}
