using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// 1 step = 1 part of the skill (called TemplateAction.cs) that can work independently.
    /// But if work together with other step, could create a actual complex skill. 
    /// E.g. projectile that explode into AOE.
    /// 
    /// 1 step could contain several SkillPart (which contain TemplateAction). 
    /// But only 1 SkillPart will be played, which'll be played depending on the trigger. 
    /// </summary>
    public class SkillFlow
    {
        private TriggerEnum _trigger;
        private List<SkillPart> _actionGroups;

        // ================================== getter ==================================
        public TriggerEnum Trigger => _trigger;
        public IReadOnlyList<SkillPart> ActionGroups => _actionGroups;

        // ================================== setter ==================================
        public SkillFlow(TriggerEnum trigger, List<SkillPart> actionGroups)
        {
            _trigger = trigger;
            _actionGroups = actionGroups;
        }

        // ================================== init ==================================
        // pass the caster down to the groups this step holds
        public void Init(IEffectable caster)
        {
            if (_actionGroups == null) return;

            foreach (SkillPart actionGroup in _actionGroups) actionGroup?.Init(caster);
        }
    }
}
