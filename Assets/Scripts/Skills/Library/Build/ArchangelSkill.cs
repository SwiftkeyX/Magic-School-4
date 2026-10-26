using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ArchangelSkill : SkillDefinition
    {
        private static float DamageReductionPercent => Tuned(30f);
        private static float AttackSpeedBuff => Tuned(20f);
        private static float BuffDuration => Tuned(4f);

        private static int AreaHexes => Tuned(3);
        private static float AreaDiameter => AreaHexes * 2f + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ArchangelSkill(registry);

        private ArchangelSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Aegis of Light";

        public override string Description
            => $"Every ally within {AreaHexes} hexes, herself included, takes {DamageReductionPercent}% less damage "
             + $"and gains {AttackSpeedBuff}% attack speed for {BuffDuration} seconds.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            ICustomModifier aegis = BundleRefresh(
                duration: BuffDuration,
                Buff(ModifierEnum.DamageReduction, DamageReductionPercent),
                Buff(
                    modifier: ModifierEnum.AS,
                    source: ScalingSourceEnum.Recipient,
                    ratios: (StatEnum.AS, AttackSpeedBuff, ScaleFromEnum.Base))
            );

            SkillPart light = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                Apply(
                    recipient: EffectRecipientEnum.AlliesInArea,
                    modifier: aegis)
            );

            return Flow(onStart: light);
        }
    }
}
