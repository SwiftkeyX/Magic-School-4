using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// 1 step = 1 part of the skill (called TemplateAction.cs) that can work independently.
    /// But if work together with other step, could create a actual complex skill. 
    /// E.g. projectile that explode into AOE.
    /// 
    /// 1 step could contain several SkillActionGroup (which contain TemplateAction). 
    /// But only 1 SkillActionGroup will be played, which'll be played depending on the trigger. 
    /// </summary>
    public class SkillStep
    {
        private TriggerEnum _trigger;
        private List<SkillActionGroup> _actionGroups;

        // ================================== getter ==================================
        public TriggerEnum Trigger => _trigger;
        public IReadOnlyList<SkillActionGroup> ActionGroups => _actionGroups;

        // ================================== setter ==================================
        public SkillStep(TriggerEnum trigger, List<SkillActionGroup> actionGroups)
        {
            _trigger = trigger;
            _actionGroups = actionGroups;
        }

        // ================================== init ==================================
        // pass the caster down to the groups this step holds
        public void Init(IEffectable caster)
        {
            if (_actionGroups == null) return;

            foreach (SkillActionGroup actionGroup in _actionGroups) actionGroup?.Init(caster);
        }
    }
}
