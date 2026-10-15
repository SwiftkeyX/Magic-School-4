using System.Collections.Generic;
using MagicSchool.Contracts;
using MagicSchool.StatScaling;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ReaperSkill : SkillDefinition
    {
        private static float MGDamagePerSnip => Tuned(100f);   // sheet: 100/150/400% AP
        private static int SnipCount => Tuned(3);
        private static float IntervalBetweenSnip => Tuned(0.2f);
        private static float TotalCastTime => IntervalBetweenSnip * (SnipCount - 1);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ReaperSkill(registry);

        private ReaperSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Harvest";

        public override string Description
            => $"Snips {SnipCount} times in quick succession, each cut carving a wedge in front of her "
             + $"that deals {MGDamagePerSnip}% AP to every enemy standing inside it.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            return Snip(registry);
        }

        private static SkillFlow Snip(TemplateActionRegistrySO registry)
        {
            AOETuning tune = TuneAOE(offset: AOEOffsetEnum.Tip);

            SkillPart snip = Part(
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

            return Flow(onStart: snip);
        }
    }
}
