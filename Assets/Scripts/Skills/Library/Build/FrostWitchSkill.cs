using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // DamageOverTime splits its ratio across the ticks, so it is handed the total the sheet's per-tick number adds up
    // to. Her slow is one instance re-applied every tick, so it refreshes instead of stacking, and lasts a tick past
    // the interval so it never drops between two ticks for someone still inside.
    internal class FrostWitchSkill : SkillDefinition
    {
        private static float DamagePerTick => Tuned(40f);
        private static float TickInterval => Tuned(0.5f);
        private static float Duration => Tuned(3f);
        private static float SlowPercent => Tuned(30f);
        private static float Diameter => Tuned(4.5f);
        private static float TotalDamage => DamagePerTick * (Duration / TickInterval);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new FrostWitchSkill(registry);

        private FrostWitchSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Frost Field";

        public override string Description
            => $"Freezes the ground under the densest enemy cluster for {Duration} seconds: "
             + $"{AP(DamagePerTick)} every {TickInterval} seconds, and enemies inside lose "
             + $"{SlowPercent}% attack speed.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ICustomModifier chill = BundleRefresh(
                TickInterval * 2f,
                Debuff(ModifierEnum.AS, ScalingSourceEnum.Recipient, (StatEnum.AS, SlowPercent, ScaleFromEnum.Base
            )));

            SkillPart field = Part(registry,
                source: ActionSourceEnum.ClusteredCircle,
                action: TemplateActionEnum.ZoneAOE,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneAOE(length: Diameter, width: Diameter),
                DamageOverTime(EffectRecipientEnum.EnemiesInArea, TickInterval, Duration, (StatEnum.AP, TotalDamage)),
                ApplyOverTime(EffectRecipientEnum.EnemiesInArea, TickInterval, Duration, chill)
            );

            return Flow(onStart: field);
        }
    }
}
