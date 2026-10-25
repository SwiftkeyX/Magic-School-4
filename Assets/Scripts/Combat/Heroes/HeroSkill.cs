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

        // ============================================== getter ==============================================
        public float GetCastTime() => _skill?.CastTime ?? 0f;
        public bool CanWalkWhileCasting() => _skill != null && _skill.CanWalkWhileCasting;
        public bool IsSkillRepeating() => _skill != null && _skill.IsRepeating;
        public bool IsSkillMovingMe() => _skill != null && _skill.IsMovingCaster;

        // Some heroes (e.g. generic dummy/tank archetypes) have no skill at all.
        public bool HasSkill => _skill != null && _skill.HasActive;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;

        // information to hero inspector panel
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

        // ============================================== active & passive skill ==============================================
        // if mana is full, play OnCast type of skill
        public bool TriggerOnCastSkill(bool isManaCapped)
        {
            if (!isManaCapped || !HasSkill) return false;

            return _skill.OnCast();
        }

        // if auto-attack, play OnAutoAttack type of skill
        public bool TriggerOnAutoAttack(ICombatant target)
        {
            if (!HasAutoAttackPassive) return false;

            return _skill.OnAutoAttack(target);
        }

        // before an auto-attack lands, the skill may pick another target 
        // e.g. Goblin Archer shoots a random enemy
        public ICombatant PickAutoAttackTarget(ICombatant target)
        {
            if (_skill == null) return target;

            return _skill.OnPickAutoAttackTarget(target) ?? target;
        }

        // an auto attack's damage, scaled by the skill (1 = unchanged)
        // e.g. Skeleton Archer's Long Shot mark
        public float AutoAttackDamageMultiplier(ICombatant target)
            => _skill == null ? 1f : _skill.AutoAttackDamageMultiplier(target);

        // at start of the combat, play OnCombat type of skill
        public bool TriggerOnCombatStart()
        {
            if (_skill == null) return false;

            return _skill.OnCombatStart();
        }

        // if hero dies, play OnHeroDied type of skill
        public void TriggerOnHeroDied(ICombatant dead)
        {
            _skill?.OnHeroDied(dead);
        }

        // if I die myself, play OnDeath type of skill
        public void TriggerOnDeath()
        {
            _skill?.OnDeath();
        }

        // if hero takes a burn tick, play OnHeroBurned type of skill
        public void TriggerOnHeroBurned(ICombatant burned)
        {
            _skill?.OnHeroBurned(burned);
        }

        // if an ally on one of my special marks deals damage, play OnMarkedAllyDealtDamage type of skill
        public void TriggerOnMarkedAllyDealtDamage(string mark, ICombatant ally, DamageEvent hit)
        {
            _skill?.OnMarkedAllyDealtDamage(mark, ally, hit);
        }

        // if I take a hit, play OnDamageTaken type of skill
        public void TriggerOnDamageTaken(DamageEvent hit)
        {
            _skill?.OnDamageTaken(hit);
        }
    }
}
