using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DryadSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(170f);
        private static float ASbuff => Tuned(25f);
        private static float WaveSize => Tuned(2f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DryadSkill(registry);

        private DryadSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Verdant Wave";

        public override string Description
            => "Sends a wave through the thickest part of the enemy line. Enemies it passes through "
             + $"take {AP(DamageRatio)}; allies it passes through are left {ASbuff}% faster for the "
             + "rest of the fight.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
            => Wave(registry);

        private static SkillFlow Wave(TemplateActionRegistrySO registry)
        {
            ICustomModifier ASBuff = BundleRefresh(
                duration: -1f,
                Buff(
                    modifier: ModifierEnum.AS,
                    source: ScalingSourceEnum.Recipient,
                    ratios: (StatEnum.AS, ASbuff, ScaleFromEnum.Base))
            );

            SkillPart wave = Part(
                registry: registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.PiercingProjectile,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneProjectile(size: WaveSize),

                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, DamageRatio)),
                Apply(
                    recipient: EffectRecipientEnum.AlliesInPath,
                    modifier: ASBuff
                )
            );

            return Flow(onStart: wave);
        }
    }
}
