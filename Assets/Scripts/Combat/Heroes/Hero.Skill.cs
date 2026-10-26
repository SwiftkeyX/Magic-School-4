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
        public bool TriggerActiveSkill(bool isManaCapped) => _skill.TriggerOnCastSkill(isManaCapped);

        private int _freeCasts;
        public void GrantFreeCast() { if (HasSkill) _freeCasts++; }
        public bool HasFreeCast => _freeCasts > 0;
        public void UseFreeCast() { if (_freeCasts > 0) _freeCasts--; }
        public bool TriggerOnAutoAttack(ICombatant target) => _skill.TriggerOnAutoAttack(target);
        public ICombatant PickAutoAttackTarget(ICombatant target) => _skill.PickAutoAttackTarget(target);
        public float AutoAttackDamageMultiplier(ICombatant target) => _skill.AutoAttackDamageMultiplier(target);
        public bool TriggerOnCombatStart()
        {
            Crit.ResetStreak(this);
            _freeCasts = 0;
            CancelRevive();
            RollPathLean();
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
        public bool IsSkillMovingMe() => _skill.IsSkillMovingMe();

        public bool IsSkillOutdated => _skillTuningVersion != SkillTuning.Version;

        public void RebuildSkill(SkillDefinition skill)
        {
            _skill = new HeroSkill(this, skill);
            _skillTuningVersion = SkillTuning.Version;
        }

        public bool HasSkill => _skill != null && _skill.HasSkill;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string SkillDescription => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;
        public string SkillDescriptionFor(bool formula) => _skill != null ? _skill.DescriptionFor(this, formula) : string.Empty;
        public string PassiveDescriptionFor(bool formula) => _skill != null ? _skill.PassiveDescriptionFor(this, formula) : string.Empty;
        public string AlignDescriptionFor(bool formula)
            => _SOData != null ? AlignText.Describe(AlignMarks, _alignPattern, AlignPatternCount, formula ? (float?)null : Magic) : string.Empty;
        public string DisplayName => (_SOData != null ? _SOData.Name : name) + (_star > HeroStar.Min ? $" ({_star}-star)" : "")
                                     + (_level > HeroLevel.Min ? $" (Lv {_level})" : "");
    }
}
