using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Skills;
using MagicSchool.StatScaling;

namespace MagicSchool.Combat.Heroes
{
    // Hero: its skill - when each part of it is triggered, and the text the Hero Panel reads.
    // A fight start rolls a new path lean and starts the crit count again (See Crit). A skill reads its numbers when
    // built, so one changed in the Skill Tuner since is rebuilt between fights.
    public partial class Hero
    {
        public bool TriggerActiveSkill(bool isManaCapped) => isManaCapped && HasSkill && _skill.OnCast();

        private int _freeCasts;
        public void GrantFreeCast() { if (HasSkill) _freeCasts++; }
        public bool HasFreeCast => _freeCasts > 0;
        public void UseFreeCast() { if (_freeCasts > 0) _freeCasts--; }
        public bool TriggerOnAutoAttack(ICombatant target) => HasAutoAttackPassive && _skill.OnAutoAttack(target);
        public ICombatant PickAutoAttackTarget(ICombatant target) => _skill.OnPickAutoAttackTarget(target) ?? target;
        public float AutoAttackDamageMultiplier(ICombatant target) => _skill.AutoAttackDamageMultiplier(target);
        public bool TriggerOnCombatStart()
        {
            Crit.ResetStreak(this);
            _freeCasts = 0;
            CancelRevive();
            RollPathLean();
            _findEnemy.KeepChosenTargetOnlyIfTied();
            return _skill.OnCombatStart();
        }
        public void TriggerOnHeroDied(ICombatant dead) => _skill.OnHeroDied(dead);
        public void TriggerOnDeath() => _skill.OnDeath();
        public void TriggerOnHeroBurned(ICombatant burned) => _skill.OnHeroBurned(burned);
        public void TriggerOnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit) => _skill.OnMarkedAllyDealtDamage(mark, ally, hit);
        public float GetCastTime() => _skill.CastTime;
        public bool CanWalkWhileCasting() => _skill.CanWalkWhileCasting;
        public bool IsSkillRepeating() => _skill.IsRepeating;
        public bool IsSkillMovingMe() => _skill.IsMovingCaster;

        public bool IsSkillOutdated => _skillTuningVersion != SkillTuning.Version;

        public void RebuildSkill(SkillDefinition skill)
        {
            SetSkill(skill);
            _skillTuningVersion = SkillTuning.Version;
        }

        private void SetSkill(SkillDefinition skill)
        {
            _skill = skill ?? new NoSkill();
            _skill.Init(this);
        }

        public bool HasSkill => _skill != null && _skill.HasActive;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string SkillDescription => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;
        public string SkillDescriptionFor(bool formula) => _skill != null ? _skill.DescribeActive(this, formula) : string.Empty;
        public string PassiveDescriptionFor(bool formula) => _skill != null ? _skill.DescribePassive(this, formula) : string.Empty;
        public string AlignDescriptionFor(bool formula)
            => _SOData != null ? AlignText.Describe(AlignMarks, _alignPattern, AlignPatternCount, formula ? (float?)null : Magic) : string.Empty;
        public string DisplayName => (_SOData != null ? _SOData.Name : name) + (_star > HeroStar.Min ? $" ({_star}-star)" : "")
                                     + (_level > HeroLevel.Min ? $" (Lv {_level})" : "");
    }
}
