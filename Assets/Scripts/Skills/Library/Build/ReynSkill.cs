using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ReynSkill
    {
        private const float ADDamagePerShot = 125f;
        private const float MGDamagePerShot = 125f;
        private const int ShotCount = 6;
        private const float IntervalBetweenShot = 0.1f;
        private const float TotalCastTime = IntervalBetweenShot * (ShotCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Shoot(registry) },
                description: $"Fires {ShotCount} shots in quick succession at the furthest enemy, each one landing for "
                           + $"{ADDamagePerShot}% AD + {MGDamagePerShot}% AP.");
        }

        private static SkillStep Shoot(TemplateActionRegistrySO registry)
        {
            ProjectileTuning tune = TuneProjectile(castTime: 0f);

            SkillActionGroup shoot = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerFirstHitProjectile,
                target: AimTargetEnum.Furthest,
                tuning: TuneFireTimingRunnerProjectile(ShotCount, FireTimingModeEnum.Sequence, IntervalBetweenShot, tune,
                                                       castTime: TotalCastTime),

                Damage(
                    recipient: EffectRecipientEnum.SameToAimTarget,
                    (StatEnum.ATK, ADDamagePerShot), (StatEnum.AP, MGDamagePerShot)
                )
            );

            return Step(trigger: TriggerEnum.OnCast, groups: shoot);
        }

    }
}