using MagicSchool.Contracts;
using MagicSchool.Skills; 

namespace MagicSchool.Combat.Heroes
{

    /// <summary>
    /// Work with SkillDefinition
    /// HeroSkill decide when to play each type of skill.
    /// e.g.    OnCast skill  =   played when mana is full
    ///         OnAttack skill    =   played when hero auto-attack
    /// </summary>
    internal class HeroSkill
    {
        private readonly SkillDefinition _skill;

        // ============================================== getter ==============================================
        public float GetCastTime() => _skill?.CastTime ?? 0f;

        // Some heroes (e.g. generic dummy/tank archetypes) have no skill at all.
        public bool HasSkill => _skill != null && _skill.HasActive;
        public bool HasPassive => _skill != null && _skill.HasPassive;

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

        // if auto-attack, play OnAttack type of skill
        public bool TriggerOnAttack(ICombatant target)
        {
            if (!HasPassive) return false;

            return _skill.OnAttack(target);
        }
    }
}
