using System;
using System.Collections.Generic;
using MagicSchool.Contracts;

namespace MagicSchool.Skills
{

    /// <summary>
    /// SkillPart contain all the data neccessary to play TemplateAction.
    /// </summary>
    public class SkillPart
    {
        private ActionSourceEnum _source;
        private TemplateAction _templateAction;
        private AimTargetEnum _target;
        private List<SkillCondition> _conditions;
        private Tuning _tuning;         

        // public Offset _offset;
        // ...

        private List<SkillEffect> _effects;    // 1 template action = have several effect

        public SkillPart(ActionSourceEnum source, TemplateAction templateAction, AimTargetEnum target,
                                List<SkillCondition> conditions = null, List<SkillEffect> effects = null,
                                Tuning tuning = null)
        {
            _source = source;
            _templateAction = templateAction;
            _target = target;
            _tuning = tuning;

            // empty rather than null, matching what the serialiser writes for an unused list
            _conditions = conditions ?? new List<SkillCondition>();
            _effects = effects ?? new List<SkillEffect>();
        }

        // ============================================ OnSkillHit ============================================
        // Called each time THIS action hits an enemy, return who it hit.
        // e.g. a projectile that pierce 3 enemies in a row, call OnSkillHit 3 times.
        public Action<ICombatant> OnSkillHit;

        // ============================================ Init ============================================
        // Inject caster into class that need it.
        public void Init(IEffectable caster)
        {
            foreach (SkillCondition condition in _conditions) condition?.Init(caster);

            foreach (SkillEffect effect in _effects) effect?.Init(caster);
        }

        // ============================================ Getter ============================================
        public ActionSourceEnum Source => _source;
        public TemplateAction TemplateAction => _templateAction;
        public AimTargetEnum Target => _target;
        public Tuning Tuning => _tuning;
        public List<SkillCondition> Conditions => _conditions;
        public List<SkillEffect> Effects => _effects;
    }
}
