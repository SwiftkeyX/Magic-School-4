using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class DryadSkill : SkillDefinition
    {
        private const float DamageRatio = 170f;   // sheet: 170/255/420% AP
        private const float ASbuff = 25f;
        private const float WaveSize = 2f;        // Dryad's wave is the one projectile bigger than default

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DryadSkill(registry);

        private DryadSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Verdant Wave";

        public override string Description
            => "Sends a wave through the thickest part of the enemy line. Enemies it passes through "
             + $"take {DamageRatio}% AP; allies it passes through are left {ASbuff}% faster for the "
             + "rest of the fight.";

        // ============================== active ==============================
        protected override List<SkillFlow> Active(TemplateActionRegistrySO registry)
            => new List<SkillFlow> { Wave(registry) };

        private static SkillFlow Wave(TemplateActionRegistrySO registry)
        {
            ICustomModifier ASBuff = Bundle(
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
                target: AimTargetEnum.ClusteredLaser,   // A/B: swap to AimTargetEnum.Clustered for the old, radial pick
                tuning: TuneProjectile(size: WaveSize),

                // deal dmg to enemy & give buff to ally 
                Damage(
                    recipient: EffectRecipientEnum.EnemiesInPath,
                    ratios: (StatEnum.AP, DamageRatio)),
                Apply(
                    recipient: EffectRecipientEnum.AlliesInPath,
                    modifier: ASBuff
                )
            );

            return Flow(trigger: TriggerEnum.OnCast, groups: wave);
        }
    }
}
