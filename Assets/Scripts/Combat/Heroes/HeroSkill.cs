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
        public bool IsSkillRepeating() => _skill != null && _skill.IsRepeating;

        // Some heroes (e.g. generic dummy/tank archetypes) have no skill at all.
        public bool HasSkill => _skill != null && _skill.HasActive;
        public bool HasAutoAttackPassive => _skill != null && _skill.HasAutoAttackPassive;

        // information to hero inspector panel
        public string SkillName => _skill != null ? _skill.SkillName : string.Empty;
        public string Description => _skill != null ? _skill.Description : string.Empty;
        public string PassiveDescription => _skill != null ? _skill.PassiveDescription : string.Empty;

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
    }
}
