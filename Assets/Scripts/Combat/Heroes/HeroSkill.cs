using System;
using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{

    /// <summary>
    /// HeroSkill contain trigger condition for each hero skill.
    /// </summary>
    internal class HeroSkill
    {
        private readonly ICombatant _me;
        private readonly SkillDefinition _skill;
        private float _castTime;

        // ============================================== getter ==============================================
        public float GetCastTime() => _castTime;

        // Some heroes (e.g. generic dummy/tank archetypes) have no skill at all.
        public bool HasSkill => _skill != null && _skill.ActiveSteps.Count > 0;
        public bool HasPassive => _skill != null && _skill.PassiveSteps.Count > 0;

        // information to hero inspector panel
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string Description => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;

        public HeroSkill(ICombatant me, SkillDefinition skill)
        {
            _me = me;
            _skill = skill;
            _skill?.Init(_me);
        }

        // ============================================== active & passive skill ==============================================
        // mana is full, OnCast skill is trigger. 
        // p.s. OnCast & OnCastStart are different, OnCast => active skill, OnCastStart => another skill that activate right after OnCast
        // p.s.2. OnCast's order is always = 0 in steps list
        public bool TriggerOnCastSkill(bool isManaCapped)
        {
            if (!isManaCapped || !HasSkill) return false;

            IReadOnlyList<SkillStep> steps = _skill.ActiveSteps;

            // OnCast skill is always step 0
            if (steps[0].Trigger != TriggerEnum.OnCast) return false;

            // fire OnCast skill first
            if (!FireStep(steps, 0)) return false;

            // then fire OnCastStart second
            TriggerOnCastStart(steps);

            return true;
        }

        // passive skill could always be triggered if the condition is true  
        public bool TriggerPassiveSkill(TriggerEnum trigger)
        {
            if (!HasPassive) return false;

            IReadOnlyList<SkillStep> steps = _skill.PassiveSteps;

            // if any trigger is matched to parameter, fired that passive skill
            if (steps[0].Trigger != trigger) return false;

            bool played = FireStep(steps, 0);

            // after fire passive skill, invoke [trigger = OnAttack] => to activate Vharn's combo counter
            // FLAGGING: Don't sure if it should stay here. Let's look at it again when the pattern is more clear. 
            if (played) _skill.InvokeTrigger(trigger);

            return played;
        }

        // ============================================== Trigger condition ==============================================
        // on kill enemy, OnKill step is trigger
        private void TriggerOnKillStep(int hp, SkillStep step)
        {
            // bool isTargetDead = hp <= 0;

            // if (isTargetDead) FireAction(IndexOfStep(step));
        }

        // This function create a event that was handed to TemplateAction.
        // on event invoke, next TemplateAction is played (if available), and related context is sent to be used for next TemplateAction.
        // e.g. Projectile invoke "OnHit" event => Send projectile hit position to next TemplateAction
        private Action<SkillStepContext> TriggerNextStep(IReadOnlyList<SkillStep> steps, int nextIndex, TriggerEnum trigger)
        {
            // guard
            if (nextIndex >= steps.Count) return null;

            // if trigger type match with this step, fire next step
            if (steps[nextIndex].Trigger != trigger) return null;

            return context => FireStep(steps, nextIndex, context);
        }

        // if OnCastStart available, it always start right after OnCast do
        // p.s. OnCastStart's order is always = 1 in steps list
        private void TriggerOnCastStart(IReadOnlyList<SkillStep> steps)
        {
            // guard
            if (steps.Count <= 1) return;

            if (steps[1].Trigger != TriggerEnum.OnCastStart) return;

            FireStep(steps, 1);
        }

        // ============================================== helper ==============================================
        // Fire skill in "step" order. Returns whether this step actually played anything.
        private bool FireStep(IReadOnlyList<SkillStep> steps, int stepIndex, SkillStepContext contextFromPreviousStep = null)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count) return false;

            // step can have several template action, only template action was choose base on condition
            return PlayAction(steps, stepIndex, contextFromPreviousStep);
        }

        // Play a ActionGroup
        private bool PlayAction(IReadOnlyList<SkillStep> steps, int stepIndex, SkillStepContext contextFromPreviousStep)
        {
            // initial event - this is handed to template action that's going to be played
            Action<SkillStepContext> onExpired = TriggerNextStep(steps, stepIndex + 1, TriggerEnum.OnExpired);
            Action<SkillStepContext> onHit = TriggerNextStep(steps, stepIndex + 1, TriggerEnum.OnHit);

            // Choose 1 TemplateAction from ActionGroup
            IReadOnlyList<SkillActionGroup> actionGroups = steps[stepIndex].ActionGroups;
            foreach (SkillActionGroup actionGroup in actionGroups)
            {
                // check condition for current TemplateAction. 
                // if condition is not met, skip this one, and go check the next template action.
                if (SkillCondition.Ask(actionGroup.Conditions, _me) == ConditionResultEnum.ConditionIsNotMet) continue;

                // try play the template action
                // FLAGGING: Beside OnExpired, OnHit, there'll be more of it. 
                // We'll need to group them and send them in 1 go via list.
                // So the parameter didn't get clustered. 
                bool played = TemplateAction.TryPlay(actionGroup, _me, onExpired, onHit, contextFromPreviousStep);

                // if one of the template action is played, stop
                if (played)
                {
                    _castTime = actionGroup.Tuning?.CastTime ?? actionGroup.TemplateAction.CastTime;
                    return true;
                }
            }

            return false;
        }
    }
}
