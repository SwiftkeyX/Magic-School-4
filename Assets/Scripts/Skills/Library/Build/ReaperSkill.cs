using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ReaperSkill : SkillDefinition
    {
        private const float MGDamagePerSnip = 100f;   // sheet: 100/150/400% AP
        private const int SnipCount = 3;
        private const float IntervalBetweenSnip = 0.2f;
        private const float TotalCastTime = IntervalBetweenSnip * (SnipCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ReaperSkill(registry);

        private ReaperSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Harvest";

        public override string Description
            => $"Snips {SnipCount} times in quick succession, each cut carving a wedge in front of her "
             + $"that deals {MGDamagePerSnip}% AP to every enemy standing inside it.";

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            return new List<SkillStep> { Snip(registry) };
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
