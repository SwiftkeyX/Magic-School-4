using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal static class AldricSkill
    {
        private const float StunDuration = 2f;
        private const float LandingDamage = 200f;
        private const float LandingSize = 4.5f;
        private const int JumpRange = 4;

        private static readonly float LandingRadius = Reach(LandingSize);

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Jump(registry), Landing(registry) },
                description: "Leaps into the densest part of the enemy formation. The landing deals "
                           + $"{LandingDamage}% AP to everyone caught around him and leaves them stunned for "
                           + $"{StunDuration} seconds.");
        }

        private static SkillStep Jump(TemplateActionRegistrySO registry)
        {
            SkillActionGroup jump = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.Move,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneMove(range: JumpRange, spread: LandingRadius)
            );

            return Step(trigger: TriggerEnum.OnCast, groups: jump);
        }

        private static SkillStep Landing(TemplateActionRegistrySO registry)
        {
            ICustomModifier stun = Bundle(
                duration: StunDuration,
                modifiers: (
                    Status(ModifierEnum.Stun)
                )
            );

            SkillActionGroup landing = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(size: LandingSize),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    ratios: (StatEnum.AP, LandingDamage)
                ),
                Apply(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    modifier: stun
                )
            );

            return Step(TriggerEnum.OnExpired, landing);
        }
    }
}