using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class MyconidSkill
    {
        private const float DamageRatio = 200f;
        // the roster sheet says "stunned" without a length - 1s is a placeholder until it does
        private const float StunDuration = 1f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = Bundle(StunDuration, Status(ModifierEnum.Stun));

            SkillActionGroup spore = ActionGroup(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.Furthest,
                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.AP, DamageRatio)),
                Apply(EffectRecipientEnum.EnemiesInPath, stun)
            );

            return new SkillDefinition(
                skillName: "Stun Spore",
                activeSteps: new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: spore) },
                description: $"Shoots a spore at the furthest enemy. Everyone in its path takes {DamageRatio}% AP "
                             + $"and is stunned for {StunDuration} seconds.");
        }
    }
}
