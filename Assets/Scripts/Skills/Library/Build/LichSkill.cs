using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class LichSkill : SkillDefinition
    {
        private const float DamageRatio = 250f;
        private const float OmnivampPercent = 40f;
        private const float OmnivampDuration = 0.5f;
        private const float BeamLength = 10f;
        private const float BeamWidth = 3f;

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new LichSkill(registry);

        private LichSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Soul Drain";

        public override string Description
            => $"Fires a piercing beam down the most crowded enemy lane: {DamageRatio}% AP to everything "
             + $"in it. He heals for {OmnivampPercent}% of the damage dealt.";

        // ============================== active ==============================
        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart beam = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.BoxAOE,
                target: AimTargetEnum.ClusteredLaser,
                tuning: TuneAOE(length: BeamLength, width: BeamWidth, offset: AOEOffsetEnum.Tip),

                Apply(
                    EffectRecipientEnum.Self,
                    BundleRefresh(
                        OmnivampDuration,
                        Buff(ModifierEnum.Omnivamp, OmnivampPercent)
                    )),

                Damage(EffectRecipientEnum.EnemiesInPath, (StatEnum.AP, DamageRatio))
            );

            return Flow(onStart: beam);
        }
    }
}
