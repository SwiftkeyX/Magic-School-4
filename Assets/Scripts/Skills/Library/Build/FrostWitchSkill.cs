using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class FrostWitchSkill : SkillDefinition
    {
        private const float DamagePerTick = 60f;
        private const float TickInterval = 0.5f;
        private const float Duration = 3f;
        private const float SlowPercent = 30f;
        private const float Diameter = 4.5f;
        // DamageOverTime splits its ratio across the ticks, so hand it the total the sheet's per-tick
        // number adds up to
        private const float TotalDamage = DamagePerTick * (Duration / TickInterval);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new FrostWitchSkill(registry);

        private FrostWitchSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Frost Field";

        public override string Description
            => $"Freezes the ground under the densest enemy cluster for {Duration} seconds: "
             + $"{DamagePerTick}% AP every {TickInterval} seconds, and enemies inside lose "
             + $"{SlowPercent}% attack speed.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
        {
            // one instance, re-applied every tick - so it refreshes instead of stacking. Lasts one tick
            // past the interval so it never drops between two ticks for someone still inside.
            ICustomModifier chill = Bundle(
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

            return new List<SkillFlow> { Flow(trigger: TriggerEnum.OnCast, groups: field) };
        }
    }
}
