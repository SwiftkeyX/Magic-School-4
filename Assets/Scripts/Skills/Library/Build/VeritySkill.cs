using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class VeritySkill
    {
        private const int Count = 4;
        private const float Interval = 0.2f;
        private const float ADDamagePerShot = 160f;
        private const float MGDamagePerShot = 60f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { DirectDamage(registry) },
                description: $"Strikes the current target {Count} times in quick succession, every blow landing for "
                           + $"{ADDamagePerShot}% AD + {MGDamagePerShot}% AP.");
        }

        private static SkillStep DirectDamage(TemplateActionRegistrySO registry)
        {
            SkillActionGroup shoot = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerCast,
                target: AimTargetEnum.Current,
                tuning: TuneFireTimingRunner(
                    Count,
                    FireTimingModeEnum.Sequence,
                    Interval,
                    null,
                    castTime: 0f
                ),

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                )
            );

            return Step(trigger: TriggerEnum.OnCast, groups: shoot);
        }
    }
}