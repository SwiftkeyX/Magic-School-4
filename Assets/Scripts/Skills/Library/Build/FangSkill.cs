using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class FangSkill
    {
        // buff
        private const float AttackSpeedBuff = 100f;   // +100%
        private const float BuffDuration = 2.5f;

        // stun
        private const float StunDuration = 2.5f;

        // heal on aa
        private const float HealOnAA = 30f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            SkillDefinition skill = new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Cast(registry), OnCastExpired(registry) },
                passiveSteps: new List<SkillStep> { OnAttack(registry) },
                description: $"Works himself into a frenzy for {BuffDuration} seconds, gaining {AttackSpeedBuff}% "
                           + "attack speed. When it wears off he lets out a howl that stuns every enemy around him "
                           + $"for {StunDuration} seconds.",
                passiveDescription: $"Every auto attack he lands heals him for {HealOnAA}% AP.");

            return skill;
        }

        // ============================== active: the transform ==============================
        private static SkillStep Cast(TemplateActionRegistrySO registry)
        {
            // increase as + 100%
            // +100% of his own attack speed - a self-referential ratio, resolved once when it lands
            ICustomModifier modifiers = Bundle(
                duration: BuffDuration,
                modifiers: Buff(ModifierEnum.AS, (StatEnum.AS, AttackSpeedBuff, ScaleFromEnum.Base))
            );

            // cast buff
            SkillActionGroup cast = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.Self, modifiers));

            return Step(trigger: TriggerEnum.OnCast, groups: cast);
        }

        private static SkillStep OnCastExpired(TemplateActionRegistrySO registry)
        {
            // stun
            ICustomModifier stun = Bundle(StunDuration, Status(ModifierEnum.Stun));

            // aoe on self 
            SkillActionGroup AOE = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                Apply(EffectRecipientEnum.EnemiesInArea, stun));

            return Step(trigger: TriggerEnum.OnExpired, groups: AOE);
        }

        // ============================== passive: the combo ==============================
        private static SkillStep OnAttack(TemplateActionRegistrySO registry)
        {
            // cast
            SkillActionGroup cast = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Cast,
                target: AimTargetEnum.Self,
                // sheet: 30% AP
                Heal(EffectRecipientEnum.Self, (StatEnum.AP, HealOnAA)));

            return Step(trigger: TriggerEnum.OnAttack, groups: cast);
        }
    }
}