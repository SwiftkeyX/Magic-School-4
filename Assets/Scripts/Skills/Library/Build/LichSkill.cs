using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    internal class LichSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(250f);
        private static float OmnivampPercent => Tuned(40f);
        private static float OmnivampDuration => Tuned(0.5f);
        private static float BeamLength => Tuned(10f);
        private static float BeamWidth => Tuned(3f);

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
