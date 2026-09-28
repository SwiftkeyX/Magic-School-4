using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class MiraSkill
    {
        private const float MGDamagePerSnip = 100f;   // sheet: 100/150/400% AP
        private const int SnipCount = 3;
        private const float IntervalBetweenSnip = 0.2f;
        private const float TotalCastTime = IntervalBetweenSnip * (SnipCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry)
        {
            return new SkillDefinition(
                skillName: "Skill",
                activeSteps: new List<SkillStep> { Snip(registry) },
                description: $"Snips {SnipCount} times in quick succession, each cut carving a wedge in front of her "
                           + $"that deals {MGDamagePerSnip}% AP to every enemy standing inside it.");
        }

        private static SkillStep Snip(TemplateActionRegistrySO registry)
        {
            AOETuning tune = TuneAOE(offset: AOEOffsetEnum.Tip);

            SkillActionGroup snip = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.FireTimingRunnerTriangleAOE,
                target: AimTargetEnum.Current,
                tuning: TuneFireTimingRunner(
                    SnipCount, FireTimingModeEnum.Sequence,
                    IntervalBetweenSnip,
                    tune,
                    castTime: TotalCastTime),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInArea,
                    (StatEnum.AP, MGDamagePerSnip)
                )
            );

            return Step(trigger: TriggerEnum.OnCast, groups: snip);
        }
    }
}
