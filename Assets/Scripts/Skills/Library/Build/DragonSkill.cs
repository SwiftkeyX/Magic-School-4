using System.Collections.Generic;
using MagicSchool.Contracts;
using static MagicSchool.Skills.SkillFactory;

namespace MagicSchool.Skills
{
    // BurnTotal is over the whole burn, not per tick. The cone's tip sits on him, pointed at the densest pack; its
    // length and width are world units.
    internal class DragonSkill : SkillDefinition
    {
        private static float DamageRatio => Tuned(200f);
        private static float BurnTotal => Tuned(50f);
        private static float BurnDuration => Tuned(3f);
        private static float BurnInterval => Tuned(1f);
        private static float ConeLength => Tuned(2.5f);
        private static float ConeWidth => Tuned(2.5f);

        public static SkillDefinition Build(TemplateActionRegistrySO registry) => new DragonSkill(registry);

        private DragonSkill(TemplateActionRegistrySO registry) : base(registry) { }

        public override string SkillName => "Dragon Breath";

        public override string Description
            => $"Breathes a cone of fire at the densest cluster: {AP(DamageRatio)}, then burns them for "
             + $"{AP(BurnTotal)} over {BurnDuration} seconds.";

        protected override SkillFlow BuildActiveFlow(TemplateActionRegistrySO registry)
        {
            SkillPart breath = Part(registry,
                source: ActionSourceEnum.Self,
                action: TemplateActionEnum.TriangleAOE,
                target: AimTargetEnum.ClusteredCircle,
                tuning: TuneAOE(length: ConeLength, width: ConeWidth, offset: AOEOffsetEnum.Tip),
                Damage(EffectRecipientEnum.EnemiesInArea, (StatEnum.AP, DamageRatio)),
                Burn(EffectRecipientEnum.EnemiesInArea, BurnInterval, BurnDuration, (StatEnum.AP, BurnTotal))
            );

            return Flow(onStart: breath);
        }
    }
}
