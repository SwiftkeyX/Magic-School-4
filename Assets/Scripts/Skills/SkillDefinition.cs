using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using UnityEngine;

namespace MagicSchool.Skills
{
    /// <summary>
    /// A whole container for 1 skill.
    /// SkillDefinition contain list of SkillFlow for both passive & active skill.
    /// skill are separated into step, those step are working together in order, to create a actual skill.
    /// </summary>
    public abstract class SkillDefinition
    {
        private TemplateActionRegistrySO _registry;

        // active skill.
        public IReadOnlyList<SkillFlow> ActiveFlows { get; private set; } = new List<SkillFlow>();

        // passive skill.
        public IReadOnlyList<SkillFlow> PassiveFlows { get; private set; } = new List<SkillFlow>();

        protected ICombatant Caster { get; private set; }   // owner of this skill
        public float CastTime { get; private set; } // the animation's duration to play this specific skill

        protected SkillDefinition(TemplateActionRegistrySO registry)
        {
            _registry = registry;
        }

        // ============================================== init ==============================================
        /// Active and Passive skill of the hero
        /// Called once from Init
        protected abstract List<SkillFlow> Active(TemplateActionRegistrySO registry);
        protected virtual List<SkillFlow> Passive(TemplateActionRegistrySO registry) => new List<SkillFlow>();

        public virtual void Init(ICombatant caster)
        {
            Caster = caster;

            if (_registry != null)
            {
                ActiveFlows = Active(_registry) ?? new List<SkillFlow>();
                PassiveFlows = Passive(_registry) ?? new List<SkillFlow>();

                // prevent second init from rebuilt the skill
                _registry = null;
            }

            foreach (SkillFlow step in ActiveFlows) step.Init(caster);

            foreach (SkillFlow step in PassiveFlows) step.Init(caster);
        }

        // ============================================== virtual ==============================================
        public abstract string SkillName { get; }
        public abstract string Description { get; }
        public virtual string PassiveDescription => "";
        public virtual bool HasActive => ActiveFlows.Count > 0;
        public virtual bool HasPassive => PassiveFlows.Count > 0;

        // ============================================== virtual hooks ==============================================
        // If mana is full, play the active skill. 
        // Returns whether anything played.
        public virtual bool OnCast()
        {
            if (!HasActive || ActiveFlows[0].Trigger != TriggerEnum.OnCast) return false;

            if (!PlayFlow(ActiveFlows, 0)) return false;

            // OnCastStart rides along with the cast
            if (ActiveFlows.Count > 1 && ActiveFlows[1].Trigger == TriggerEnum.OnCastStart) PlayFlow(ActiveFlows, 1);

            return true;
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
            if (PassiveFlows.Count == 0 || PassiveFlows[0].Trigger != trigger) return false;

            return PlayFlow(PassiveFlows, 0);
        }

        // Play one step of a skill chain
        private bool PlayFlow(IReadOnlyList<SkillFlow> steps, int stepIndex, Vector3? previousPosition = null)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count) return false;

            // init the callback 
            Action<Vector3> onExpired = NextFlow(steps, stepIndex + 1, TriggerEnum.OnExpired);
            Action<Vector3> onHit = NextFlow(steps, stepIndex + 1, TriggerEnum.OnHit);

            // play the first action group that met the condition
            foreach (SkillPart group in steps[stepIndex].ActionGroups)
            {
                if (SkillCondition.Ask(group.Conditions, Caster) == ConditionResultEnum.ConditionIsNotMet) continue;

                // play the template action
                // and give callback to the template action
                var callbacks = new TemplateActionCallbacks
                {
                    OnExpired = onExpired,
                    OnHit = onHit,
                    OnSkillHit = group.OnSkillHit,
                };

                if (Play(group, callbacks, previousPosition)) return true;
            }

            return false;
        }

        // Play a single group on its own.
        // no step, no chain skill. 
        protected bool PlayGroup(SkillPart group, ICombatant assignedTarget = null)
            => Play(
                group, 
                new TemplateActionCallbacks { OnSkillHit = group.OnSkillHit }, 
                null,
                assignedTarget
            );

        // try play template action
        // wiring NextFlow() callback to template action
        private bool Play(SkillPart group, TemplateActionCallbacks callbacks, Vector3? previousPosition,
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
        // e.g.     the AOE is gone (the prefab is destroyed)   =   call NextFlow(OnExpired)
        //          the moment projectile hit something         =   call NextFlow(OnHit)
        private Action<Vector3> NextFlow(IReadOnlyList<SkillFlow> steps, int nextIndex, TriggerEnum trigger)
        {
            if (nextIndex >= steps.Count) return null;

            if (steps[nextIndex].Trigger != trigger) return null;

            return position => PlayFlow(steps, nextIndex, position);
        }
    }
}
