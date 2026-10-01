using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class ArchangelSkill : SkillDefinition
    {
        private const float DamageReductionPercent = 30f;
        private const float AttackSpeedBuff = 20f;
        private const float BuffDuration = 4f;

        private const int AreaHexes = 3;
        private const float AreaDiameter = AreaHexes * 2f + 0.5f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new ArchangelSkill(registry);

        private ArchangelSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Aegis of Light";

        public override string Description
            => $"Every ally within {AreaHexes} hexes, herself included, takes {DamageReductionPercent}% less damage "
             + $"and gains {AttackSpeedBuff}% attack speed for {BuffDuration} seconds.";

        // ============================== active ==============================
        protected override List<SkillStep> Active(TemplateActionRegistrySO registry)
        {
            ICustomModifier aegis = Bundle(
                duration: BuffDuration,
                Buff(ModifierEnum.DamageReduction, DamageReductionPercent),
                Buff(
                    modifier: ModifierEnum.AS,
                    source: ScalingSourceEnum.Recipient,
                    ratios: (StatEnum.AS, AttackSpeedBuff, ScaleFromEnum.Base))
            );

            SkillActionGroup light = ActionGroup(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.CircleAOE,
                target: AimTargetEnum.Self,
                tuning: TuneAOE(length: AreaDiameter, width: AreaDiameter),

                Apply(
                    recipient: EffectRecipientEnum.AlliesInArea,
                    modifier: aegis)
            );

            return new List<SkillStep> { Step(trigger: TriggerEnum.OnCast, groups: light) };
        }
    }
}
