using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Skills;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Heroes
{
    // Hero: its skill - when each part of it is triggered, and the text the Hero Panel reads.
    public partial class Hero
    {
        // ======================================== skill ========================================
        public bool TriggerActiveSkill(bool isManaCapped) => _skill.TriggerOnCastSkill(isManaCapped);

        // ======================================== free cast ========================================
        // casts another unit's skill gave him, played without mana (See IFreeCaster)
        // e.g.     Reaper's kills let the allies on her Reap mark cast
        private int _freeCasts;
        public void GrantFreeCast() { if (HasSkill) _freeCasts++; }
        public bool HasFreeCast => _freeCasts > 0;
        public void UseFreeCast() { if (_freeCasts > 0) _freeCasts--; }
        public bool TriggerOnAutoAttack(ICombatant target) => _skill.TriggerOnAutoAttack(target);
        public ICombatant PickAutoAttackTarget(ICombatant target) => _skill.PickAutoAttackTarget(target);
        // a fight starts: the crit count starts again too (read Crit.cs)
        public bool TriggerOnCombatStart()
        {
            Crit.ResetStreak(this);
            _freeCasts = 0;
            _findEnemy.KeepChosenTargetOnlyIfTied();
            return _skill.TriggerOnCombatStart();
        }
        public void TriggerOnHeroDied(ICombatant dead) => _skill.TriggerOnHeroDied(dead);
        public void TriggerOnDeath() => _skill.TriggerOnDeath();
        public void TriggerOnHeroBurned(ICombatant burned) => _skill.TriggerOnHeroBurned(burned);
        public void TriggerOnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit) => _skill?.TriggerOnMarkedAllyDealtDamage(mark, ally, hit);
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
