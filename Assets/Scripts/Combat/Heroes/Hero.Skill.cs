using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{
    // Hero: its skill - when each part of it is triggered, and the text the Hero Panel reads.
    public partial class Hero
    {
        // ======================================== skill ========================================
        public bool TriggerActiveSkill(bool isManaCapped) => _skill.TriggerOnCastSkill(isManaCapped);
        public bool TriggerOnAutoAttack(ICombatant target) => _skill.TriggerOnAutoAttack(target);
        public ICombatant PickAutoAttackTarget(ICombatant target) => _skill.PickAutoAttackTarget(target);
        public bool TriggerOnCombatStart() => _skill.TriggerOnCombatStart();
        public void TriggerOnHeroDied(ICombatant dead) => _skill.TriggerOnHeroDied(dead);
        public void TriggerOnDeath() => _skill.TriggerOnDeath();
        public void TriggerOnHeroBurned(ICombatant burned) => _skill.TriggerOnHeroBurned(burned);
        public float GetCastTime() => _skill.GetCastTime();
        public bool CanWalkWhileCasting() => _skill.CanWalkWhileCasting();
        public bool IsSkillRepeating() => _skill.IsSkillRepeating();

        // a skill reads its numbers once, when built; Skill Tuner changed one since
        public bool IsSkillOutdated => _skillTuningVersion != SkillTuning.Version;

        // swap in a freshly built skill, so a tuned number takes effect. Only between fights.
        public void RebuildSkill(SkillDefinition skill)
        {
            _skill = new HeroSkill(this, skill);
            _skillTuningVersion = SkillTuning.Version;
        }

        // what the Hero Panel reads
        public bool HasSkill => _skill != null && _skill.HasSkill;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string SkillDescription => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;
        public string AlignDescription => _SOData != null ? AlignText.Describe(AlignMarks, _alignPattern, AlignPatternCount) : string.Empty;
        public string DisplayName => (_SOData != null ? _SOData.Name : name) + (_star > HeroStar.Min ? $" ({_star}-star)" : "");
    }
}
