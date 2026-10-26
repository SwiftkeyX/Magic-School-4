using MagicSchool.Contracts;
using MagicSchool.Skills;

namespace MagicSchool.Combat.Heroes
{

    /// <summary>
    /// Work with SkillDefinition
    /// HeroSkill decide when to play each type of skill.
    /// e.g.    OnCast skill    =   played when mana is full
    ///         OnAutoAttack skill  =   played when hero auto-attack
    /// </summary>
    internal class HeroSkill
    {
        private readonly SkillDefinition _skill;

        public float GetCastTime() => _skill?.CastTime ?? 0f;
        public bool CanWalkWhileCasting() => _skill != null && _skill.CanWalkWhileCasting;
        public bool IsSkillRepeating() => _skill != null && _skill.IsRepeating;
        public bool IsSkillMovingMe() => _skill != null && _skill.IsMovingCaster;

        public bool HasSkill => _skill != null && _skill.HasActive;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;

        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string Description => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;
        public string DescriptionFor(IHeroStats stats, bool formula) => _skill != null ? _skill.DescribeActive(stats, formula) : string.Empty;
        public string PassiveDescriptionFor(IHeroStats stats, bool formula) => _skill != null ? _skill.DescribePassive(stats, formula) : string.Empty;

        public HeroSkill(ICombatant me, SkillDefinition skill)
        {
            _skill = skill;
            _skill?.Init(me);
        }

        public bool TriggerOnCastSkill(bool isManaCapped)
        {
            if (!isManaCapped || !HasSkill) return false;

            return _skill.OnCast();
        }

        public bool TriggerOnAutoAttack(ICombatant target)
        {
            if (!HasAutoAttackPassive) return false;

            return _skill.OnAutoAttack(target);
        }

        public ICombatant PickAutoAttackTarget(ICombatant target)
        {
            if (_skill == null) return target;

            return _skill.OnPickAutoAttackTarget(target) ?? target;
        }

        public float AutoAttackDamageMultiplier(ICombatant target)
            => _skill == null ? 1f : _skill.AutoAttackDamageMultiplier(target);

        public bool TriggerOnCombatStart()
        {
            if (_skill == null) return false;

            return _skill.OnCombatStart();
        }

        public void TriggerOnHeroDied(ICombatant dead)
        {
            _skill?.OnHeroDied(dead);
        }

        public void TriggerOnDeath()
        {
            _skill?.OnDeath();
        }

        public void TriggerOnHeroBurned(ICombatant burned)
        {
            _skill?.OnHeroBurned(burned);
        }

        public void TriggerOnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit)
        {
            _skill?.OnMarkedAllyDealtDamage(mark, ally, hit);
        }

        public void TriggerOnDamageTaken(DamageEvent hit)
        {
            _skill?.OnDamageTaken(hit);
        }
    }
}
